using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FitLife.Maui.Services;

/// <summary>
/// Represents a single bike in the spinning room (4 rows × 4 bikes = 16 total).
/// </summary>
/// <remarks>
/// This is an <see cref="ObservableObject"/> on purpose: the bike grid is updated
/// <b>in place</b> after a selection (see <c>LessonDetailViewModel.LoadBikesAsync</c>)
/// instead of clearing and rebuilding the backing collection. Rebuilding the
/// CollectionView's ItemsSource from inside a tapped item's command crashes the
/// native list renderer (the tapped button is destroyed mid-gesture). Keeping the
/// 16 items stable and only mutating their state avoids that entirely — which is
/// why <see cref="IsAvailable"/> and <see cref="IsSelectedByCurrentUser"/> must
/// raise change notifications for the computed colours below.
/// </remarks>
public partial class BikeItem : ObservableObject
{
    [JsonPropertyName("rowNumber")]
    public int RowNumber { get; set; }

    [JsonPropertyName("bikeNumber")]
    public int BikeNumber { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundColor))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    [NotifyPropertyChangedFor(nameof(StrokeColor))]
    [NotifyPropertyChangedFor(nameof(StrokeThickness))]
    [property: JsonPropertyName("isAvailable")]
    private bool _isAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundColor))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    [NotifyPropertyChangedFor(nameof(StrokeColor))]
    [NotifyPropertyChangedFor(nameof(StrokeThickness))]
    [property: JsonPropertyName("isSelectedByCurrentUser")]
    private bool _isSelectedByCurrentUser;

    // Sequential number shown on the button (1–16), row-major order
    [JsonIgnore]
    public string DisplayLabel => ((RowNumber - 1) * 4 + BikeNumber).ToString();

    // Visual colours driven by availability state
    [JsonIgnore]
    public Color BackgroundColor => IsSelectedByCurrentUser
        ? Color.FromArgb("#C5E17A")   // green — own bike
        : IsAvailable
            ? Color.FromArgb("#F5F5F5") // light gray — free
            : Color.FromArgb("#FFCDD2"); // light red — taken

    [JsonIgnore]
    public Color TextColor => IsSelectedByCurrentUser
        ? Color.FromArgb("#1A1A1A")
        : IsAvailable
            ? Color.FromArgb("#333333")
            : Color.FromArgb("#B71C1C");

    // Border emphasises the user's own selected bike so it stands out from free/taken seats
    [JsonIgnore]
    public Color StrokeColor => IsSelectedByCurrentUser
        ? Color.FromArgb("#2E7D32")   // strong green outline — own bike
        : IsAvailable
            ? Color.FromArgb("#CCCCCC") // neutral gray — free
            : Color.FromArgb("#E59A9A"); // muted red — taken

    [JsonIgnore]
    public double StrokeThickness => IsSelectedByCurrentUser ? 2.5 : 1;
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
