using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

/// <summary>
/// Result returned by the reservation API after a cancel/reserve attempt.
/// Mirrors the JSON shape produced by the FitLife.API endpoints.
/// </summary>
public class ReservationActionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("remainingCredits")]
    public int? RemainingCredits { get; set; }
}

/// <summary>
/// DTO for a lesson the user has reserved, returned by GET /users/{userId}/lessons.
/// </summary>
public class UserLessonDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("workoutName")]
    public string WorkoutName { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("instructorName")]
    public string InstructorName { get; set; } = string.Empty;

    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = string.Empty;
}

/// <summary>
/// Contract for the MAUI client-side reservation service.
/// Encapsulates the HTTP calls to the FitLife.API reservation endpoints.
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// Returns all active (non-cancelled) lesson reservations for the given user.
    /// </summary>
    Task<IEnumerable<UserLessonDto>> GetUserLessonsAsync(int userId);

    /// <summary>
    /// Cancels the active reservation of the given user for the given lesson on the server.
    /// </summary>
    Task<ReservationActionResult> CancelReservationAsync(int lessonId, int userId);
}

/// <summary>
/// HTTP-based implementation of <see cref="IReservationService"/>.
/// Talks to the FitLife.API over the configured <see cref="HttpClient"/>.
/// </summary>
public class ReservationService : IReservationService
{
    private readonly HttpClient _httpClient;

    public ReservationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Calls GET /users/{userId}/lessons and returns the user's active reservations.
    /// Returns an empty list on any error so the caller never needs to handle exceptions.
    /// </summary>
    public async Task<IEnumerable<UserLessonDto>> GetUserLessonsAsync(int userId)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"users/{userId}/lessons");
            if (response.IsSuccessStatusCode)
            {
                var lessons = await response.Content.ReadFromJsonAsync<IEnumerable<UserLessonDto>>();
                return lessons ?? Enumerable.Empty<UserLessonDto>();
            }

            System.Diagnostics.Debug.WriteLine($"[ReservationService] GetUserLessonsAsync: {(int)response.StatusCode}");
            return Enumerable.Empty<UserLessonDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReservationService] GetUserLessonsAsync error: {ex.Message}");
            return Enumerable.Empty<UserLessonDto>();
        }
    }

    /// <summary>
    /// Sends a DELETE request to <c>/lessons/{lessonId}/cancel?userId={userId}</c>.
    /// Returns a structured <see cref="ReservationActionResult"/>, never throws on
    /// expected error paths (network errors are mapped to a failure result).
    /// </summary>
    public async Task<ReservationActionResult> CancelReservationAsync(int lessonId, int userId)
    {
        try
        {
            // The API expects the userId as a query string parameter.
            var requestUri = $"lessons/{lessonId}/cancel?userId={userId}";

            using var response = await _httpClient.DeleteAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                // The endpoint returns { success, message, remainingCredits }.
                var result = await response.Content.ReadFromJsonAsync<ReservationActionResult>();
                return result ?? new ReservationActionResult
                {
                    Success = false,
                    Message = "Onbekend antwoord van de server."
                };
            }

            return new ReservationActionResult
            {
                Success = false,
                Message = $"Server gaf statuscode {(int)response.StatusCode} terug."
            };
        }
        catch (Exception ex)
        {
            // Log and translate any transport-level error to a user-friendly failure result.
            System.Diagnostics.Debug.WriteLine($"[ReservationService] CancelReservationAsync error: {ex.Message}");
            return new ReservationActionResult
            {
                Success = false,
                Message = "Netwerkfout: kon de reservering niet annuleren."
            };
        }
    }
}