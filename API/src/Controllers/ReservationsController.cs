using API.Mappers;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationResponse>>> GetAll()
        {
            var result = await _reservationService.GetAllReservationsAsync();
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(ReservationMapper.ToResponses(result.Value!));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationResponse>> GetById(int id)
        {
            var result = await _reservationService.GetReservationByIdAsync(id);
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            if (result.Value == null)
                return NotFound();

            return Ok(ReservationMapper.ToResponse(result.Value));
        }

        [HttpGet("member/{memberId}")]
        public async Task<ActionResult<IEnumerable<ReservationResponse>>> GetByMember(int memberId)
        {
            var result = await _reservationService.GetMemberReservationsAsync(memberId);
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(ReservationMapper.ToResponses(result.Value!));
        }

        [HttpPost]
        public async Task<ActionResult<ReservationResponse>> Create(ReservationCreateRequest request)
        {
            var result = await _reservationService.CreateReservationAsync(request.MemberId, request.LessonId);
            
            if (result.IsFailure)
            {
                if (result.Error == "Les niet gevonden." || result.Error == "Lid niet gevonden.")
                    return NotFound(new { error = result.Error });
                
                return BadRequest(new { error = result.Error });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, ReservationMapper.ToResponse(result.Value));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _reservationService.CancelReservationAsync(id);
            
            if (result.IsFailure)
            {
                if (result.Error == "Reservering niet gevonden.")
                    return NotFound(new { error = result.Error });
                
                return BadRequest(new { error = result.Error });
            }

            return NoContent();
        }
    }
}
