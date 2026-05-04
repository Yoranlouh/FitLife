using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Interfaces
{
    public interface IInstructorRepository
    {
        Task<ResultOf<IReadOnlyList<Instructor>>> GetAllAsync();
        Task<ResultOf<Instructor?>> GetByIdAsync(int id);
        Task<ResultOf<Instructor>> AddAsync(Instructor instructor);
        Task<ResultOf<bool>> UpdateAsync(Instructor instructor);
        Task<ResultOf<bool>> DeleteAsync(int id);
    }
}
