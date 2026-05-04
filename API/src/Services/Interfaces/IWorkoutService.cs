using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface IWorkoutService
    {
        Task<ResultOf<IReadOnlyList<Workout>>> GetAllWorkoutsAsync();
        Task<ResultOf<Workout?>> GetWorkoutByIdAsync(int id);
        Task<ResultOf<Workout>> CreateWorkoutAsync(Workout workout);
        Task<ResultOf<bool>> UpdateWorkoutAsync(Workout workout);
        Task<ResultOf<bool>> DeleteWorkoutAsync(int id);
    }
}
