using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface ILessonApi
{
    Task<IReadOnlyList<LessonResponse>> GetLessonsAsync(CancellationToken ct = default);
    Task<LessonResponse?> GetLessonByIdAsync(int id, CancellationToken ct = default);
    Task<LessonResponse?> CreateLessonAsync(LessonCreateRequest request, CancellationToken ct = default);
    Task<bool> UpdateLessonAsync(int id, LessonUpdateRequest request, CancellationToken ct = default);
    Task<bool> DeleteLessonAsync(int id, CancellationToken ct = default);
}
