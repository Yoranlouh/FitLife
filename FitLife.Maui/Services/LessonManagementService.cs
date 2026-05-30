using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

public class SimpleItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class LessonSaveRequest
{
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("maxParticipants")]
    public int MaxParticipants { get; set; }

    [JsonPropertyName("workoutId")]
    public int WorkoutId { get; set; }

    [JsonPropertyName("instructorId")]
    public int InstructorId { get; set; }

    [JsonPropertyName("locationId")]
    public int LocationId { get; set; }
}

public class AddMemberRequest
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
}

public interface ILessonManagementService
{
    Task<IEnumerable<LessonResponse>> GetInstructorLessonsAsync(int instructorId);
    Task<IEnumerable<SimpleItemDto>> GetWorkoutsAsync();
    Task<IEnumerable<SimpleItemDto>> GetLocationsAsync();
    Task<IEnumerable<SimpleItemDto>> GetInstructorsAsync();
    Task<(bool Success, string Message)> CreateLessonAsync(LessonSaveRequest request);
    Task<(bool Success, string Message)> UpdateLessonAsync(int lessonId, LessonSaveRequest request);
    Task<(bool Success, string Message)> DeleteLessonAsync(int lessonId);
    Task<(bool Success, string Message)> AddMemberToLessonAsync(int lessonId, int userId);
}

public class LessonManagementService : ILessonManagementService
{
    private readonly HttpClient _httpClient;

    public LessonManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<LessonResponse>> GetInstructorLessonsAsync(int instructorId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"lessons/instructor/{instructorId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<IEnumerable<LessonResponse>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching instructor lessons: {ex.Message}");
        }
        return [];
    }

    public async Task<IEnumerable<SimpleItemDto>> GetWorkoutsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("workouts");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<IEnumerable<SimpleItemDto>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching workouts: {ex.Message}");
        }
        return [];
    }

    public async Task<IEnumerable<SimpleItemDto>> GetLocationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("locations");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<IEnumerable<SimpleItemDto>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching locations: {ex.Message}");
        }
        return [];
    }

    public async Task<IEnumerable<SimpleItemDto>> GetInstructorsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("instructors");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<IEnumerable<SimpleItemDto>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching instructors: {ex.Message}");
        }
        return [];
    }

    public async Task<(bool Success, string Message)> CreateLessonAsync(LessonSaveRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("lessons", request);
            if (response.IsSuccessStatusCode)
                return (true, "Les succesvol aangemaakt.");
            var body = await response.Content.ReadAsStringAsync();
            return (false, $"Aanmaken mislukt: {body}");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateLessonAsync(int lessonId, LessonSaveRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"lessons/{lessonId}", request);
            if (response.IsSuccessStatusCode)
                return (true, "Les succesvol bijgewerkt.");
            var body = await response.Content.ReadAsStringAsync();
            return (false, $"Bijwerken mislukt: {body}");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteLessonAsync(int lessonId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"lessons/{lessonId}");
            if (response.IsSuccessStatusCode)
                return (true, "Les succesvol verwijderd.");
            var body = await response.Content.ReadAsStringAsync();
            return (false, $"Verwijderen mislukt: {body}");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> AddMemberToLessonAsync(int lessonId, int userId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"lessons/{lessonId}/add-member", new AddMemberRequest { UserId = userId });
            if (response.IsSuccessStatusCode)
                return (true, "Lid toegevoegd aan de les.");
            var body = await response.Content.ReadAsStringAsync();
            return (false, $"Toevoegen mislukt: {body}");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }
}