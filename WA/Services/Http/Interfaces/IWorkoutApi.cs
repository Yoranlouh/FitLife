using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace WA.Services.Http.Interfaces;

public interface IWorkoutApi
{
    Task<IReadOnlyList<WorkoutResponse>> GetWorkoutsAsync(CancellationToken ct = default);
    Task<WorkoutResponse?> GetWorkoutByIdAsync(int id, CancellationToken ct = default);
    Task<WorkoutResponse?> CreateWorkoutAsync(WorkoutCreateRequest request, CancellationToken ct = default);
    Task<bool> UpdateWorkoutAsync(int id, WorkoutUpdateRequest request, CancellationToken ct = default);
    Task<bool> DeleteWorkoutAsync(int id, CancellationToken ct = default);
}
