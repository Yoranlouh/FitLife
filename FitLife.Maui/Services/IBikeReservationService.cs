using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

/// <summary>
/// Represents a single bike in the spinning room (4 rows × 4 bikes = 16 total).
/// </summary>
public class BikeItem
{
    [JsonPropertyName("rowNumber")]
    public int RowNumber { get; set; }

    [JsonPropertyName("bikeNumber")]
    public int BikeNumber { get; set; }

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("isSelectedByCurrentUser")]
    public bool IsSelectedByCurrentUser { get; set; }

    // Sequential number shown on the button (1–16), row-major order
    public string DisplayLabel => ((RowNumber - 1) * 4 + BikeNumber).ToString();

    // Visual colours driven by availability state
    public Color BackgroundColor => IsSelectedByCurrentUser
        ? Color.FromArgb("#C5E17A")   // green — own bike
        : IsAvailable
            ? Color.FromArgb("#F5F5F5") // light gray — free
            : Color.FromArgb("#FFCDD2"); // light red — taken

    public Color TextColor => IsSelectedByCurrentUser
        ? Color.FromArgb("#1A1A1A")
        : IsAvailable
            ? Color.FromArgb("#333333")
            : Color.FromArgb("#B71C1C");
}

/// <summary>Result returned by bike reservation API calls.</summary>
public class BikeReservationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("rowNumber")]
    public int? RowNumber { get; set; }

    [JsonPropertyName("bikeNumber")]
    public int? BikeNumber { get; set; }
}

/// <summary>Contract for spinning-lesson bike reservation operations.</summary>
public interface IBikeReservationService
{
    /// <summary>Returns the 16-bike grid with availability for the given lesson.</summary>
    Task<List<BikeItem>> GetBikesAsync(int lessonId, int userId);

    /// <summary>Reserves (or changes) the user's bike via POST /lessons/{id}/bikes.</summary>
    Task<BikeReservationResult> SelectBikeAsync(int lessonId, int userId, int rowNumber, int bikeNumber);

    /// <summary>Releases the user's bike reservation via DELETE /lessons/{id}/bikes.</summary>
    Task<BikeReservationResult> ReleaseBikeAsync(int lessonId, int userId);
}

/// <summary>HTTP-based implementation of <see cref="IBikeReservationService"/>.</summary>
public class BikeReservationService : IBikeReservationService
{
    private readonly HttpClient _httpClient;

    public BikeReservationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BikeItem>> GetBikesAsync(int lessonId, int userId)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"lessons/{lessonId}/bikes?userId={userId}");
            if (response.IsSuccessStatusCode)
            {
                var bikes = await response.Content.ReadFromJsonAsync<List<BikeItem>>();
                return bikes ?? new List<BikeItem>();
            }
            return new List<BikeItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BikeReservationService] GetBikesAsync error: {ex.Message}");
            return new List<BikeItem>();
        }
    }

    public async Task<BikeReservationResult> SelectBikeAsync(int lessonId, int userId, int rowNumber, int bikeNumber)
    {
        try
        {
            var body = new { userId, rowNumber, bikeNumber };
            using var response = await _httpClient.PostAsJsonAsync($"lessons/{lessonId}/bikes", body);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BikeReservationResult>();
                return result ?? new BikeReservationResult { Success = false, Message = "Onbekend antwoord van de server." };
            }
            return new BikeReservationResult { Success = false, Message = $"Server gaf statuscode {(int)response.StatusCode} terug." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BikeReservationService] SelectBikeAsync error: {ex.Message}");
            return new BikeReservationResult { Success = false, Message = "Netwerkfout bij reserveren van fiets." };
        }
    }

    public async Task<BikeReservationResult> ReleaseBikeAsync(int lessonId, int userId)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync($"lessons/{lessonId}/bikes?userId={userId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BikeReservationResult>();
                return result ?? new BikeReservationResult { Success = true };
            }
            return new BikeReservationResult { Success = false, Message = $"Server gaf statuscode {(int)response.StatusCode} terug." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BikeReservationService] ReleaseBikeAsync error: {ex.Message}");
            return new BikeReservationResult { Success = false, Message = "Netwerkfout bij vrijgeven van fiets." };
        }
    }
}
