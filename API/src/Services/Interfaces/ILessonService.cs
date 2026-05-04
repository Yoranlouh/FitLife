using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface ILessonService
    {
        Task<ResultOf<IReadOnlyList<Lesson>>> GetAllLessonsAsync();
        Task<ResultOf<Lesson?>> GetLessonByIdAsync(int id);
        Task<ResultOf<Lesson>> CreateLessonAsync(Lesson lesson);
        Task<ResultOf<bool>> UpdateLessonAsync(Lesson lesson);
        Task<ResultOf<bool>> DeleteLessonAsync(int id);
    }
}