using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface IInstructorApi
{
    Task<IReadOnlyList<InstructorResponse>> GetInstructorsAsync(CancellationToken ct = default);
    Task<InstructorResponse?> GetInstructorByIdAsync(int id, CancellationToken ct = default);
    Task<InstructorResponse?> CreateInstructorAsync(InstructorCreateRequest request, CancellationToken ct = default);
    Task<bool> UpdateInstructorAsync(int id, InstructorUpdateRequest request, CancellationToken ct = default);
    Task<bool> DeleteInstructorAsync(int id, CancellationToken ct = default);
    Task<InstructorResponse?> UploadPhotoAsync(int id, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
}
