using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace FitLife.Maui.Services;

// Contract for fetching the full lesson catalogue from the API.
public interface ILessonService
{
    // Returns lessons from the API. userId marks booked lessons; from/to restrict the date range.
    Task<IEnumerable<LessonResponse>> GetLessonsAsync(int? userId = null, DateTime? from = null, DateTime? to = null);
}

// HTTP implementation that calls GET /lessons on the FitLife REST API.
public class LessonService : ILessonService
{
    private readonly HttpClient _httpClient;

    public LessonService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Explicit options: case-insensitive so "workoutColor" from the API maps to WorkoutColor in C#,
    // regardless of .NET version differences in ReadFromJsonAsync default behaviour.
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    // Fetches lessons from GET /lessons with optional user and date range filters.
    // Returns an empty collection on any error so callers never need to handle null.
    public async Task<IEnumerable<LessonResponse>> GetLessonsAsync(int? userId = null, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var queryParts = new List<string>();
            if (userId.HasValue && userId.Value > 0) queryParts.Add($"userId={userId.Value}");
            if (from.HasValue) queryParts.Add($"from={Uri.EscapeDataString(from.Value.ToString("s"))}");
            if (to.HasValue)   queryParts.Add($"to={Uri.EscapeDataString(to.Value.ToString("s"))}");
            var url = queryParts.Count > 0 ? $"lessons?{string.Join("&", queryParts)}" : "lessons";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<LessonResponse>>(_jsonOptions)
                       ?? Enumerable.Empty<LessonResponse>();
            }

            var body = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(
                $"[LessonService] HTTP {(int)response.StatusCode} from {url}: {body}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LessonService] Netwerkfout bij ophalen lessen: {ex.Message}");
        }

        return Enumerable.Empty<LessonResponse>();
    }
}
