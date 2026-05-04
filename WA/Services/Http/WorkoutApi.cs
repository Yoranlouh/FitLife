using System.Net.Http.Json;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;
using WA.Services.Http.Interfaces;

namespace WA.Services.Http;

public sealed class WorkoutApi : IWorkoutApi
{
    private readonly HttpClient _http;

    public WorkoutApi(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<WorkoutResponse>> GetWorkoutsAsync(CancellationToken ct = default)
    {
        var workouts = await _http.GetFromJsonAsync<List<WorkoutResponse>>("api/workouts", ct);
        return workouts ?? [];
    }

    public async Task<WorkoutResponse?> GetWorkoutByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<WorkoutResponse>($"api/workouts/{id}", ct);
    }

    public async Task<WorkoutResponse?> CreateWorkoutAsync(WorkoutCreateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/workouts", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WorkoutResponse>(cancellationToken: ct);
        }
        return null;
    }

    public async Task<bool> UpdateWorkoutAsync(int id, WorkoutUpdateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/workouts/{id}", request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteWorkoutAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/workouts/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
