using API.Mappers;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/workouts")]
    public class WorkoutsController : ControllerBase
    {
        private readonly IWorkoutService _workoutService;

        public WorkoutsController(IWorkoutService workoutService)
        {
            _workoutService = workoutService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutResponse>>> GetAll()
        {
            var result = await _workoutService.GetAllWorkoutsAsync();
            
            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            return Ok(WorkoutMapper.ToResponses(result.Value!));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutResponse>> GetById(int id)
        {
            var result = await _workoutService.GetWorkoutByIdAsync(id);

            if (result.IsFailure)
                return StatusCode(500, new { error = result.Error });

            if (result.Value == null)
                return NotFound();

            return Ok(WorkoutMapper.ToResponse(result.Value));
        }

        [HttpPost]
        public async Task<ActionResult<WorkoutResponse>> Create(WorkoutCreateRequest request)
        {
            var workout = WorkoutMapper.ToEntity(request);
            var result = await _workoutService.CreateWorkoutAsync(workout);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, WorkoutMapper.ToResponse(result.Value));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, WorkoutUpdateRequest request)
        {
            var existingResult = await _workoutService.GetWorkoutByIdAsync(id);
            
            if (existingResult.IsFailure)
                return StatusCode(500, new { error = existingResult.Error });

            if (existingResult.Value == null)
                return NotFound();

            var workout = existingResult.Value;
            WorkoutMapper.UpdateEntity(workout, request);

            var updateResult = await _workoutService.UpdateWorkoutAsync(workout);
            
            if (updateResult.IsFailure)
                return BadRequest(new { error = updateResult.Error });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _workoutService.DeleteWorkoutAsync(id);

            if (result.IsFailure)
            {
                if (result.Error == "Workout not found")
                    return NotFound();
                
                return StatusCode(500, new { error = result.Error });
            }

            return NoContent();
        }
    }
}
