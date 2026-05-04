using API.Mappers;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LessonResponse>>> GetAll()
        {
            var result = await _lessonService.GetAllLessonsAsync();
            
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(LessonMapper.ToResponses(result.Value!));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LessonResponse>> GetById(int id)
        {
            var result = await _lessonService.GetLessonByIdAsync(id);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            if (result.Value == null)
                return NotFound();

            return Ok(LessonMapper.ToResponse(result.Value));
        }

        [HttpPost]
        public async Task<ActionResult<LessonResponse>> Create(LessonCreateRequest request)
        {
            var lesson = LessonMapper.ToEntity(request);
            var result = await _lessonService.CreateLessonAsync(lesson);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, LessonMapper.ToResponse(result.Value));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LessonUpdateRequest request)
        {
            var existingResult = await _lessonService.GetLessonByIdAsync(id);
            
            if (existingResult.IsFailure)
                return StatusCode(500, new { error = existingResult.Error });

            if (existingResult.Value == null)
                return NotFound();

            var lesson = existingResult.Value;
            LessonMapper.UpdateEntity(lesson, request);

            var updateResult = await _lessonService.UpdateLessonAsync(lesson);
            
            if (updateResult.IsFailure)
                return BadRequest(new { error = updateResult.Error });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _lessonService.DeleteLessonAsync(id);

            if (result.IsFailure)
            {
                if (result.Error == "Lesson not found")
                    return NotFound();
                
                return StatusCode(500, new { error = result.Error });
            }

            return NoContent();
        }
    }
}