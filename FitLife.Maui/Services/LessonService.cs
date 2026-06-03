using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;

namespace FitLife.Maui.Services;

// Contract for fetching the full lesson catalogue from the API.
public interface ILessonService
{
    // Returns all upcoming lessons from the API (used by the schedule pages).
    Task<IEnumerable<LessonResponse>> GetLessonsAsync();
}

// HTTP implementation that calls GET /lessons on the FitLife REST API.
public class LessonService : ILessonService
{
    private readonly HttpClient _httpClient;

    public LessonService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Fetches the complete lesson list from GET /lessons.
    // Returns an empty collection on any error so callers never need to handle null.
    public async Task<IEnumerable<LessonResponse>> GetLessonsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("lessons");
            if (response.IsSuccessStatusCode)
            {
                // ReadFromJsonAsync deserialises the JSON array into strongly-typed objects
                return await response.Content.ReadFromJsonAsync<IEnumerable<LessonResponse>>()
                       ?? Enumerable.Empty<LessonResponse>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching lessons: {ex.Message}");
        }

        return Enumerable.Empty<LessonResponse>();
    }
}
