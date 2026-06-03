using FitLife.BlazorWebApp.Models;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for lesson management operations
/// </summary>
public interface ILessonService
{
    Task<List<LessonDto>> GetAllLessonsAsync(DateTime? fromDate = null, DateTime? toDate = null, bool archivedOnly = false);
    Task<List<LessonDto>> GetLessonsByInstructorAsync(int instructorId, DateTime? fromDate = null);
    Task<LessonDto?> GetLessonByIdAsync(int lessonId);
    Task<(bool Success, string Message)> CreateLessonAsync(LessonEditDto lesson);
    Task<(bool Success, string Message)> UpdateLessonAsync(LessonEditDto lesson);
    Task<(bool Success, string Message)> DeleteLessonAsync(int lessonId);
    Task<List<ReservationDto>> GetLessonParticipantsAsync(int lessonId);
}