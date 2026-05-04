using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Interfaces
{
    public interface IWorkoutRepository
    {
        Task<ResultOf<IReadOnlyList<Workout>>> GetAllAsync();
        Task<ResultOf<Workout?>> GetByIdAsync(int id);
        Task<ResultOf<Workout>> AddAsync(Workout workout);
        Task<ResultOf<bool>> UpdateAsync(Workout workout);
        Task<ResultOf<bool>> DeleteAsync(int id);
    }
}
