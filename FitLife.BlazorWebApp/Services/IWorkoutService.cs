namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Interface for workout type management
/// </summary>
public interface IWorkoutService
{
    Task<List<WorkoutDto>> GetAllWorkoutsAsync();
    Task<WorkoutDto?> GetWorkoutByIdAsync(int workoutId);
    Task<(bool Success, string Message)> CreateWorkoutAsync(WorkoutDto workout);
    Task<(bool Success, string Message)> UpdateWorkoutAsync(WorkoutDto workout);
    Task<(bool Success, string Message)> DeleteWorkoutAsync(int workoutId);
}