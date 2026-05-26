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
/// Contract for the MAUI client-side reservation service.
/// Encapsulates the HTTP calls to the FitLife.API reservation endpoints.
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// Cancels the active reservation of the given user for the given lesson on the server.
    /// </summary>
    /// <param name="lessonId">Identifier of the lesson whose reservation should be cancelled.</param>
    /// <param name="userId">Identifier of the user that owns the reservation.</param>
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