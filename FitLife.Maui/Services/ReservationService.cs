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

    // True when the reserve call failed specifically because the lesson is at full capacity.
    // The caller should then automatically join the waitlist instead of showing a plain error.
    [JsonPropertyName("lessonFull")]
    public bool LessonFull { get; set; }

    // True when the waitlist join was rejected because the user is already on it.
    [JsonPropertyName("alreadyOnWaitlist")]
    public bool AlreadyOnWaitlist { get; set; }

    // Position on the waitlist returned by POST /lessons/{id}/waitlist.
    [JsonPropertyName("position")]
    public int? Position { get; set; }
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
/// </summary>
public interface IReservationService
{
    /// <summary>Returns all active (non-cancelled) reservations for the given user.</summary>
    Task<IEnumerable<UserLessonDto>> GetUserLessonsAsync(int userId);

    /// <summary>Creates a reservation and deducts 1 credit via POST /lessons/{lessonId}/reserve.</summary>
    Task<ReservationActionResult> ReserveAsync(int lessonId, int userId);

    /// <summary>Cancels a reservation via DELETE /lessons/{lessonId}/cancel.</summary>
    Task<ReservationActionResult> CancelReservationAsync(int lessonId, int userId);

    /// <summary>Adds the user to the waitlist via POST /lessons/{lessonId}/waitlist.</summary>
    Task<ReservationActionResult> JoinWaitlistAsync(int lessonId, int userId);
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
    /// Calls POST /lessons/{lessonId}/reserve with { userId }.
    /// Deducts 1 credit and creates a reservation in the database.
    /// </summary>
    public async Task<ReservationActionResult> ReserveAsync(int lessonId, int userId)
    {
        try
        {
            var body = new { userId };
            using var response = await _httpClient.PostAsJsonAsync(
                $"lessons/{lessonId}/reserve", body);

            if (response.IsSuccessStatusCode)
            {
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
            System.Diagnostics.Debug.WriteLine($"[ReservationService] ReserveAsync error: {ex.Message}");
            return new ReservationActionResult
            {
                Success = false,
                Message = "Netwerkfout: kon de reservering niet opslaan."
            };
        }
    }

    /// <summary>
    /// Calls GET /users/{userId}/lessons and returns the user's active reservations.
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
    /// Calls POST /lessons/{lessonId}/waitlist with { userId }.
    /// Returns the user's position on the waitlist on success.
    /// </summary>
    public async Task<ReservationActionResult> JoinWaitlistAsync(int lessonId, int userId)
    {
        try
        {
            var body = new { userId };
            using var response = await _httpClient.PostAsJsonAsync(
                $"lessons/{lessonId}/waitlist", body);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ReservationActionResult>();
                return result ?? new ReservationActionResult { Success = false, Message = "Onbekend antwoord van de server." };
            }

            return new ReservationActionResult { Success = false, Message = $"Server gaf statuscode {(int)response.StatusCode} terug." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReservationService] JoinWaitlistAsync error: {ex.Message}");
            return new ReservationActionResult { Success = false, Message = "Netwerkfout: kon niet worden aangemeld voor de wachtlijst." };
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