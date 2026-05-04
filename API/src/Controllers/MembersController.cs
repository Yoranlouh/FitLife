using API.Mappers;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/members")]
    public class MembersController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MembersController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberResponse>>> GetAll()
        {
            var result = await _memberService.GetAllMembersAsync();
            
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(MemberMapper.ToResponses(result.Value!));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MemberResponse>> GetById(int id)
        {
            var result = await _memberService.GetMemberByIdAsync(id);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            if (result.Value == null)
                return NotFound();

            return Ok(MemberMapper.ToResponse(result.Value));
        }

        [HttpPost]
        public async Task<ActionResult<MemberResponse>> Create(MemberCreateRequest request)
        {
            var member = MemberMapper.ToEntity(request);
            var result = await _memberService.CreateMemberAsync(member);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, MemberMapper.ToResponse(result.Value));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, MemberUpdateRequest request)
        {
            var existingResult = await _memberService.GetMemberByIdAsync(id);
            
            if (existingResult.IsFailure)
                return StatusCode(500, new { error = existingResult.Error });

            if (existingResult.Value == null)
                return NotFound();

            var member = existingResult.Value;
            MemberMapper.UpdateEntity(member, request);

            var updateResult = await _memberService.UpdateMemberAsync(member);
            
            if (updateResult.IsFailure)
                return BadRequest(new { error = updateResult.Error });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _memberService.DeleteMemberAsync(id);

            if (result.IsFailure)
            {
                if (result.Error == "Member not found")
                    return NotFound();
                
                return StatusCode(500, new { error = result.Error });
            }

            return NoContent();
        }

        [HttpPost("{id}/photo")]
        public async Task<ActionResult<MemberResponse>> UploadPhoto(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var result = await _memberService.UpdateMemberPhotoAsync(id, stream, file.ContentType, file.FileName);

            if (result.IsFailure)
            {
                if (result.Error == "Member not found")
                    return NotFound();
                
                return BadRequest(new { error = result.Error });
            }

            return Ok(MemberMapper.ToResponse(result.Value!));
        }

        [HttpGet("{id}/profile")]
        public async Task<ActionResult<MemberResponse>> GetProfile(int id)
        {
            return await GetById(id);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var existingResult = await _memberService.GetMemberByIdAsync(id);
            if (existingResult.IsFailure || existingResult.Value == null)
                return existingResult.Value == null ? NotFound() : StatusCode(500, new { error = existingResult.Error });

            var member = existingResult.Value;
            member.Status = status;

            var updateResult = await _memberService.UpdateMemberAsync(member);
            if (updateResult.IsFailure)
                return BadRequest(new { error = updateResult.Error });

            return NoContent();
        }
    }
}
