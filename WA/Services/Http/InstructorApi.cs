using System.Net.Http.Headers;
using System.Net.Http.Json;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;
using WA.Services.Http.Interfaces;

namespace WA.Services.Http;

public sealed class InstructorApi : IInstructorApi
{
    private readonly HttpClient _http;

    public InstructorApi(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<InstructorResponse>> GetInstructorsAsync(CancellationToken ct = default)
    {
        var instructors = await _http.GetFromJsonAsync<List<InstructorResponse>>("api/instructors", ct);
        return instructors ?? [];
    }

    public async Task<InstructorResponse?> GetInstructorByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<InstructorResponse>($"api/instructors/{id}", ct);
    }

    public async Task<InstructorResponse?> CreateInstructorAsync(InstructorCreateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/instructors", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<InstructorResponse>(cancellationToken: ct);
        }
        return null;
    }

    public async Task<bool> UpdateInstructorAsync(int id, InstructorUpdateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/instructors/{id}", request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteInstructorAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/instructors/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<InstructorResponse?> UploadPhotoAsync(int id, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await _http.PostAsync($"api/instructors/{id}/photo", content, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<InstructorResponse>(cancellationToken: ct);
        }
        return null;
    }
}
