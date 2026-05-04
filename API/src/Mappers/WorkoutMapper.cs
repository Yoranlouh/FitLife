using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class WorkoutMapper
    {
        public static WorkoutResponse ToResponse(Workout workout)
        {
            return new WorkoutResponse
            {
                Id = workout.Id,
                Name = workout.Name,
                Description = workout.Description,
                Duration = workout.Duration
            };
        }

        public static IEnumerable<WorkoutResponse> ToResponses(IEnumerable<Workout> workouts)
        {
            return workouts.Select(ToResponse);
        }

        public static Workout ToEntity(WorkoutCreateRequest request)
        {
            return new Workout
            {
                Name = request.Name,
                Description = request.Description,
                Duration = request.Duration
            };
        }

        public static void UpdateEntity(Workout workout, WorkoutUpdateRequest request)
        {
            workout.Name = request.Name;
            workout.Description = request.Description;
            workout.Duration = request.Duration;
        }
    }
}
