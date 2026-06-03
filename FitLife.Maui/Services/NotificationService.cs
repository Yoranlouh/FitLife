using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FitLife.Maui.Services;

// Singleton implementation of INotificationService.
// Maintains an in-memory list that is loaded from the REST API on login,
// so notifications survive app restarts and re-logins.
// All write operations (Add, MarkAllRead) update the in-memory list synchronously
// and then fire-and-forget an async HTTP request to persist the change.
public class NotificationService : INotificationService
{
    private readonly List<AppNotification>   _items = new();
    private readonly HttpClient              _httpClient;
    private readonly IAuthenticationService  _authenticationService;

    public NotificationService(HttpClient httpClient, IAuthenticationService authenticationService)
    {
        _httpClient           = httpClient;
        _authenticationService = authenticationService;
    }

    // Exposes the list as read-only so callers cannot mutate it directly
    public IReadOnlyList<AppNotification> Notifications => _items.AsReadOnly();

    // Computed from the list on every access — no separate counter to keep in sync
    public int UnreadCount => _items.Count(n => !n.IsRead);

    // Fetches the user's notification history from GET /users/{userId}/notifications.
    // Replaces the in-memory list entirely so it always matches the database.
    // Called once per login from LoginViewModel.
    public async Task LoadAsync(int userId)
    {
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<NotificationApiDto>>(
                $"users/{userId}/notifications");

            _items.Clear();
            if (dtos != null)
                foreach (var dto in dtos)
                    _items.Add(new AppNotification
                    {
                        Id        = Guid.TryParse(dto.Id, out var g) ? g : Guid.NewGuid(),
                        Title     = dto.Title,
                        Message   = dto.Message,
                        Type      = (NotificationType)dto.Type,
                        IsRead    = dto.IsRead,
                        CreatedAt = dto.CreatedAt
                    });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationService] LoadAsync fout: {ex.Message}");
        }
    }

    // Inserts a new notification at the top of the list (newest first),
    // then asynchronously saves it to the database via POST /users/{id}/notifications.
    public void Add(string title, string message, NotificationType type)
    {
        var n = new AppNotification
        {
            Title     = title,
            Message   = message,
            Type      = type,
            IsRead    = false,
            CreatedAt = DateTime.Now
        };
        _items.Insert(0, n);  // prepend so the newest item appears at the top

        var userId = _authenticationService.CurrentUserId;
        if (userId.HasValue)
            _ = SaveToApiAsync(userId.Value, n);  // fire-and-forget — don't block the caller
    }

    // Marks every in-memory notification as read, then persists the change
    // via PUT /users/{id}/notifications/mark-all-read.
    public void MarkAllRead()
    {
        foreach (var n in _items)
            n.IsRead = true;

        var userId = _authenticationService.CurrentUserId;
        if (userId.HasValue)
            _ = MarkAllReadApiAsync(userId.Value);  // fire-and-forget
    }

    // Posts a single notification to the REST API to persist it in the database.
    // Uses ON DUPLICATE KEY UPDATE so retrying is idempotent.
    private async Task SaveToApiAsync(int userId, AppNotification n)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"users/{userId}/notifications", new NotificationApiDto
            {
                Id        = n.Id.ToString(),
                Title     = n.Title,
                Message   = n.Message,
                Type      = (int)n.Type,
                IsRead    = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationService] SaveToApiAsync fout: {ex.Message}");
        }
    }

    // Calls PUT /users/{userId}/notifications/mark-all-read to update the database.
    private async Task MarkAllReadApiAsync(int userId)
    {
        try
        {
            await _httpClient.PutAsync($"users/{userId}/notifications/mark-all-read", null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationService] MarkAllReadApiAsync fout: {ex.Message}");
        }
    }
}

// Internal DTO for (de)serialising notifications from/to the REST API.
// Maps to the NotificationDto class defined in FitLife.API/Program.cs.
internal class NotificationApiDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    // Stored as an int in the database (matches the NotificationType enum values 0–4)
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
