using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;

namespace FitLife.Maui.Services;

public interface ILessonService
{
    Task<IEnumerable<LessonResponse>> GetLessonsAsync();
}

public class LessonService : ILessonService
{
    private readonly HttpClient _httpClient;

    public LessonService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<LessonResponse>> GetLessonsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("lessons");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<LessonResponse>>() ?? Enumerable.Empty<LessonResponse>();
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error fetching lessons: {ex.Message}");
        }

        return Enumerable.Empty<LessonResponse>();
    }
}
