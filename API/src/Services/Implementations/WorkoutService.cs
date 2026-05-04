using API.Domain.Common;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using SharedLibrary.Domain.Entities;

namespace API.Services.Implementations
{
    public class WorkoutService : IWorkoutService
    {
        private readonly IWorkoutRepository _repository;

        public WorkoutService(IWorkoutRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResultOf<IReadOnlyList<Workout>>> GetAllWorkoutsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResultOf<Workout?>> GetWorkoutByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ResultOf<Workout>> CreateWorkoutAsync(Workout workout)
        {
            return await _repository.AddAsync(workout);
        }

        public async Task<ResultOf<bool>> UpdateWorkoutAsync(Workout workout)
        {
            return await _repository.UpdateAsync(workout);
        }

        public async Task<ResultOf<bool>> DeleteWorkoutAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
