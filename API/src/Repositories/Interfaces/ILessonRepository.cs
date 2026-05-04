using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Repositories.Interfaces
{
    public interface ILessonRepository
    {
        Task<ResultOf<IReadOnlyList<Lesson>>> GetAllAsync();
        Task<ResultOf<Lesson?>> GetByIdAsync(int id);
        Task<ResultOf<Lesson>> AddAsync(Lesson lesson);
        Task<ResultOf<IEnumerable<Lesson>>> AddRangeAsync(IEnumerable<Lesson> lessons);
        Task<ResultOf<bool>> UpdateAsync(Lesson lesson);
        Task<ResultOf<bool>> DeleteAsync(int id);
    }
}