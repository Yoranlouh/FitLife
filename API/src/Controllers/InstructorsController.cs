using API.Mappers;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/instructors")]
    public class InstructorsController : ControllerBase
    {
        private readonly IInstructorService _instructorService;

        public InstructorsController(IInstructorService instructorService)
        {
            _instructorService = instructorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InstructorResponse>>> GetAll()
        {
            var result = await _instructorService.GetAllInstructorsAsync();
            
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(InstructorMapper.ToResponses(result.Value!));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InstructorResponse>> GetById(int id)
        {
            var result = await _instructorService.GetInstructorByIdAsync(id);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            if (result.Value == null)
                return NotFound();

            return Ok(InstructorMapper.ToResponse(result.Value));
        }

        [HttpPost]
        public async Task<ActionResult<InstructorResponse>> Create(InstructorCreateRequest request)
        {
            var instructor = InstructorMapper.ToEntity(request);
            var result = await _instructorService.CreateInstructorAsync(instructor);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, InstructorMapper.ToResponse(result.Value));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, InstructorUpdateRequest request)
        {
            var existingResult = await _instructorService.GetInstructorByIdAsync(id);
            
            if (existingResult.IsFailure)
                return StatusCode(500, new { error = existingResult.Error });

            if (existingResult.Value == null)
                return NotFound();

            var instructor = existingResult.Value;
            InstructorMapper.UpdateEntity(instructor, request);

            var updateResult = await _instructorService.UpdateInstructorAsync(instructor);
            
            if (updateResult.IsFailure)
                return BadRequest(new { error = updateResult.Error });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _instructorService.DeleteInstructorAsync(id);

            if (result.IsFailure)
            {
                if (result.Error == "Instructor not found")
                    return NotFound();
                
                return StatusCode(500, new { error = result.Error });
            }

            return NoContent();
        }

        [HttpPost("{id}/photo")]
        public async Task<ActionResult<InstructorResponse>> UploadPhoto(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var result = await _instructorService.UpdateInstructorPhotoAsync(id, stream, file.ContentType, file.FileName);

            if (result.IsFailure)
            {
                if (result.Error == "Instructor not found")
                    return NotFound();
                
                return BadRequest(new { error = result.Error });
            }

            return Ok(InstructorMapper.ToResponse(result.Value!));
        }
    }
}
