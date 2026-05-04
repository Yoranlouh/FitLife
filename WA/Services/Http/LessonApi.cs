using System.Net.Http.Json;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;
using WA.Services.Http.Interfaces;

namespace WA.Services.Http;

public sealed class LessonApi : ILessonApi
{
    private readonly HttpClient _http;

    public LessonApi(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<LessonResponse>> GetLessonsAsync(CancellationToken ct = default)
    {
        var lessons = await _http.GetFromJsonAsync<List<LessonResponse>>("api/lessons", ct);
        return lessons ?? [];
    }

    public async Task<LessonResponse?> GetLessonByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<LessonResponse>($"api/lessons/{id}", ct);
    }

    public async Task<LessonResponse?> CreateLessonAsync(LessonCreateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/lessons", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<LessonResponse>(cancellationToken: ct);
        }
        return null;
    }

    public async Task<bool> UpdateLessonAsync(int id, LessonUpdateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/lessons/{id}", request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteLessonAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/lessons/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
