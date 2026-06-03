using SharedLibrary.DTOs.Responses;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

// Simple DTO used for dropdown lists (workouts, locations, instructors).
// Matches the JSON shape returned by the API's dropdown endpoints.
public class SimpleItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

// Request body for creating or updating a lesson
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

// Request body for the add-member-to-lesson endpoint
public class AddMemberRequest
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
}

// Generic API success/failure response wrapper
public class ApiResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// Contract for all lesson management operations (CRUD + add member).
// Used by both the instructor and admin flows in the MAUI app.
public interface ILessonManagementService
{
    // Returns all lessons taught by a specific instructor
    Task<IEnumerable<LessonResponse>> GetInstructorLessonsAsync(int instructorId);

    // Returns all workout types for the dropdown selector
    Task<IEnumerable<SimpleItemDto>> GetWorkoutsAsync();

    // Returns all locations/halls for the dropdown selector
    Task<IEnumerable<SimpleItemDto>> GetLocationsAsync();

    // Returns all instructors for the dropdown selector (admin only)
    Task<IEnumerable<SimpleItemDto>> GetInstructorsAsync();

    // Creates a new lesson via the API, returns (success, message)
    Task<(bool Success, string Message)> CreateLessonAsync(LessonSaveRequest request);

    // Updates an existing lesson via the API, returns (success, message)
    Task<(bool Success, string Message)> UpdateLessonAsync(int lessonId, LessonSaveRequest request);

    // Deletes a lesson (only if no active reservations), returns (success, message)
    Task<(bool Success, string Message)> DeleteLessonAsync(int lessonId);

    // Manually adds a member to a lesson without deducting credits (admin feature)
    Task<(bool Success, string Message)> AddMemberToLessonAsync(int lessonId, int userId);
}

// HTTP implementation that communicates with the FitLife REST API.
public class LessonManagementService : ILessonManagementService
{
    private readonly HttpClient _httpClient;

    public LessonManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Calls GET /lessons/instructor/{instructorId} — returns all lessons for one trainer
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

    // Thin wrappers that delegate to the shared helper below
    public async Task<IEnumerable<SimpleItemDto>> GetWorkoutsAsync()
        => await FetchDropdownAsync("workouts");

    public async Task<IEnumerable<SimpleItemDto>> GetLocationsAsync()
        => await FetchDropdownAsync("locations");

    public async Task<IEnumerable<SimpleItemDto>> GetInstructorsAsync()
        => await FetchDropdownAsync("instructors");

    // Generic helper that fetches a JSON array from a relative endpoint and
    // deserialises it into a list of SimpleItemDto. Returns an empty list on any error.
    private async Task<IEnumerable<SimpleItemDto>> FetchDropdownAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine(
                    $"[FetchDropdown] {endpoint} → HTTP {(int)response.StatusCode}: {body}");
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[FetchDropdown] {endpoint} → {json}");

            // PropertyNameCaseInsensitive handles both camelCase and PascalCase JSON keys
            var items = System.Text.Json.JsonSerializer.Deserialize<List<SimpleItemDto>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return items ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FetchDropdown] {endpoint} → fout: {ex.Message}");
            return [];
        }
    }

    // Sends POST /lessons with the lesson details. Returns (success, message) from the API.
    public async Task<(bool Success, string Message)> CreateLessonAsync(LessonSaveRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("lessons", request);
            var result   = await response.Content.ReadFromJsonAsync<ApiResult>();
            if (result is not null)
                return (result.Success, result.Message ?? "Les aangemaakt.");
            return (response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode ? "Les succesvol aangemaakt." : "Aanmaken mislukt.");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }

    // Sends PUT /lessons/{lessonId} to update an existing lesson.
    public async Task<(bool Success, string Message)> UpdateLessonAsync(int lessonId, LessonSaveRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"lessons/{lessonId}", request);
            var result   = await response.Content.ReadFromJsonAsync<ApiResult>();
            if (result is not null)
                return (result.Success, result.Message ?? "Les bijgewerkt.");
            return (response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode ? "Les succesvol bijgewerkt." : "Bijwerken mislukt.");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }

    // Sends DELETE /lessons/{lessonId}. The API rejects this if active reservations exist.
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

    // Sends POST /lessons/{lessonId}/add-member — adds a member without credit deduction.
    public async Task<(bool Success, string Message)> AddMemberToLessonAsync(int lessonId, int userId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"lessons/{lessonId}/add-member", new AddMemberRequest { UserId = userId });
            var result = await response.Content.ReadFromJsonAsync<ApiResult>();
            if (result is not null)
                return (result.Success, result.Message ?? "Lid toegevoegd.");
            return (response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode ? "Lid succesvol toegevoegd." : "Toevoegen mislukt.");
        }
        catch (Exception ex)
        {
            return (false, $"Netwerkfout: {ex.Message}");
        }
    }
}
