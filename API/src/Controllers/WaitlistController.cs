using API.Mappers;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/waitlist")]
    public class WaitlistController : ControllerBase
    {
        private readonly IWaitlistService _waitlistService;

        public WaitlistController(IWaitlistService waitlistService)
        {
            _waitlistService = waitlistService;
        }

        [HttpPost]
        public async Task<ActionResult<WaitlistResponse>> Join(WaitlistJoinRequest request)
        {
            var result = await _waitlistService.JoinWaitlistAsync(request.MemberId, request.LessonId);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            // Fetch again to ensure includes are loaded for the response
            var entryResult = await _waitlistService.GetWaitlistEntryByIdAsync(result.Value!.Id);
            
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, WaitlistMapper.ToResponse(entryResult.Value!));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Leave(int id)
        {
            var result = await _waitlistService.LeaveWaitlistAsync(id);

            if (result.IsFailure)
            {
                if (result.Error == "Waitlist entry not found")
                    return NotFound();
                
                return StatusCode(500, new { error = result.Error });
            }

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WaitlistResponse>> GetById(int id)
        {
            var result = await _waitlistService.GetWaitlistEntryByIdAsync(id);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            if (result.Value == null)
                return NotFound();

            return Ok(WaitlistMapper.ToResponse(result.Value));
        }

        [HttpGet("lesson/{lessonId}")]
        public async Task<ActionResult<IEnumerable<WaitlistResponse>>> GetByLesson(int lessonId)
        {
            var result = await _waitlistService.GetWaitlistForLessonAsync(lessonId);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(WaitlistMapper.ToResponses(result.Value!));
        }

        [HttpGet("member/{memberId}")]
        public async Task<ActionResult<IEnumerable<WaitlistResponse>>> GetByMember(int memberId)
        {
            var result = await _waitlistService.GetMemberWaitlistEntriesAsync(memberId);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(WaitlistMapper.ToResponses(result.Value!));
        }

        [HttpPost("trigger-notification/{lessonId}")]
        public async Task<IActionResult> TriggerNotification(int lessonId)
        {
            var result = await _waitlistService.TriggerNotificationAsync(lessonId);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(new { success = result.Value, message = result.Value ? "Notification sent to first member in waitlist" : "No members on waitlist" });
        }
    }
}
