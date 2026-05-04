using API.Domain.Common;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using API.Storage.Interfaces;
using SharedLibrary.Domain.Entities;

namespace API.Services.Implementations
{
    public class InstructorService : IInstructorService
    {
        private readonly IInstructorRepository _repository;
        private readonly IPhotoStorage _photoStorage;

        public InstructorService(IInstructorRepository repository, IPhotoStorage photoStorage)
        {
            _repository = repository;
            _photoStorage = photoStorage;
        }

        public async Task<ResultOf<IReadOnlyList<Instructor>>> GetAllInstructorsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResultOf<Instructor?>> GetInstructorByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ResultOf<Instructor>> CreateInstructorAsync(Instructor instructor)
        {
            return await _repository.AddAsync(instructor);
        }

        public async Task<ResultOf<bool>> UpdateInstructorAsync(Instructor instructor)
        {
            return await _repository.UpdateAsync(instructor);
        }

        public async Task<ResultOf<bool>> DeleteInstructorAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<ResultOf<Instructor>> UpdateInstructorPhotoAsync(int instructorId, Stream photoStream, string contentType, string fileName)
        {
            var instructorResult = await _repository.GetByIdAsync(instructorId);
            if (instructorResult.IsFailure) return ResultOf<Instructor>.Failure(instructorResult.Error);
            if (instructorResult.Value == null) return ResultOf<Instructor>.Failure("Instructor not found");

            var instructor = instructorResult.Value;

            try
            {
                var saveResult = await _photoStorage.SaveAsync(
                    photoStream, 
                    contentType, 
                    fileName, 
                    "instructors", 
                    CancellationToken.None, 
                    instructorId
                );

                instructor.PhotoId = int.Parse(saveResult.Id);
                await _repository.UpdateAsync(instructor);

                // Reload to get Photo object
                var finalResult = await _repository.GetByIdAsync(instructorId);
                return ResultOf<Instructor>.Success(finalResult.Value!);
            }
            catch (Exception ex)
            {
                return ResultOf<Instructor>.Failure(ex.Message);
            }
        }
    }
}
