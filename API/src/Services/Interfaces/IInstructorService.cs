using API.Domain.Common;
using SharedLibrary.Domain.Entities;

namespace API.Services.Interfaces
{
    public interface IInstructorService
    {
        Task<ResultOf<IReadOnlyList<Instructor>>> GetAllInstructorsAsync();
        Task<ResultOf<Instructor?>> GetInstructorByIdAsync(int id);
        Task<ResultOf<Instructor>> CreateInstructorAsync(Instructor instructor);
        Task<ResultOf<bool>> UpdateInstructorAsync(Instructor instructor);
        Task<ResultOf<bool>> DeleteInstructorAsync(int id);
        Task<ResultOf<Instructor>> UpdateInstructorPhotoAsync(int instructorId, Stream photoStream, string contentType, string fileName);
    }
}
