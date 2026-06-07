namespace FitLife.Maui.Services;

// The five notification types used throughout the app.
// Each type maps to a distinct icon and colour in the UI.
public enum NotificationType
{
    LessonReserved,       // User successfully reserved a lesson
    Waitlist,             // User joined the waitlist for a full lesson
    NewWorkout,           // A new workout type was added to the schedule
    LessonCancelled,      // A lesson the user was enrolled in was cancelled
    SubscriptionExpiring  // The user's subscription expires within 7 days
}

// Domain model for a single in-app notification.
// Instances are stored in the database and loaded into memory on login.
public class AppNotification
{
    public Guid   Id        { get; set; } = Guid.NewGuid();
    public string Title     { get; set; } = "";
    public string Message   { get; set; } = "";
    public NotificationType Type     { get; set; }
    public DateTime         CreatedAt { get; set; } = DateTime.Now;
    public bool             IsRead    { get; set; }

    // Returns a single emoji character representing the notification type (used as avatar)
    public string TypeIcon => Type switch
    {
        NotificationType.LessonReserved       => "✓",
        NotificationType.Waitlist             => "⏱",
        NotificationType.NewWorkout           => "★",
        NotificationType.LessonCancelled      => "✕",
        NotificationType.SubscriptionExpiring => "!",
        _                                     => "•"
    };

    // Returns the brand colour for the notification type (used to tint the icon circle)
    public Microsoft.Maui.Graphics.Color TypeColor => Type switch
    {
        NotificationType.LessonReserved       => Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50"),
        NotificationType.Waitlist             => Microsoft.Maui.Graphics.Color.FromArgb("#FF9800"),
        NotificationType.NewWorkout           => Microsoft.Maui.Graphics.Color.FromArgb("#2196F3"),
        NotificationType.LessonCancelled      => Microsoft.Maui.Graphics.Color.FromArgb("#F44336"),
        NotificationType.SubscriptionExpiring => Microsoft.Maui.Graphics.Color.FromArgb("#FF6F00"),
        _                                     => Microsoft.Maui.Graphics.Color.FromArgb("#666666")
    };

    // Returns a human-readable relative time string, e.g. "5 min geleden" or "12 okt"
    public string TimeDisplay
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 1) return "Zojuist";
            if (diff.TotalHours   < 1) return $"{(int)diff.TotalMinutes} min geleden";
            if (diff.TotalDays    < 1) return $"{(int)diff.TotalHours} uur geleden";
            return CreatedAt.ToString("d MMM");
        }
    }
}

// Contract for the notification service.
// The service keeps an in-memory list (populated from the database on login)
// and syncs writes back to the API asynchronously.
public interface INotificationService
{
    // Read-only snapshot of the current in-memory notification list
    IReadOnlyList<AppNotification> Notifications { get; }

    // Number of notifications not yet read by the user
    int UnreadCount { get; }

    // Loads the user's notifications from the database via the REST API.
    // Must be called after login so the list reflects persisted history.
    Task LoadAsync(int userId);

    // Adds a notification to the in-memory list AND saves it to the database
    // for the given userId. Only call this for the user who actually performed the action.
    // The database write is fire-and-forget so it never blocks the UI.
    void Add(int userId, string title, string message, NotificationType type);

    // Marks all in-memory notifications as read and persists the state to the database.
    void MarkAllRead();

    // Clears the in-memory list immediately. Must be called on logout so that the
    // next account that logs in on this device never sees a previous user's notifications.
    void Clear();
}
