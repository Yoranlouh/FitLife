using FitLife.BlazorWebApp.Models;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for instructor management operations
/// </summary>
public interface IInstructorService
{
    Task<List<InstructorDto>> GetAllInstructorsAsync();
    Task<InstructorDto?> GetInstructorByIdAsync(int instructorId);
    Task<InstructorDto?> GetInstructorByUserIdAsync(int userId);
    Task<(bool Success, string Message)> CreateInstructorAsync(InstructorDto instructor);
    Task<(bool Success, string Message)> UpdateInstructorAsync(InstructorDto instructor);
    Task<(bool Success, string Message)> DeleteInstructorAsync(int instructorId);
}