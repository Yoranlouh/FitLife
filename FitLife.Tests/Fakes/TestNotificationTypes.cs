namespace FitLife.Tests.Fakes;

/// <summary>
/// Mirrors the NotificationType enum from FitLife.Maui.Services.NotificationType,
/// without the MAUI project dependency.
/// </summary>
public enum TestNotificationType
{
    LessonReserved,
    Waitlist,
    NewWorkout,
    LessonCancelled,
    SubscriptionExpiring,
    AttendanceCheckedIn
}

/// <summary>
/// Pure-C# replica of the AppNotification.TypeIcon switch expression and expected
/// TypeColor hex values. Used to test the icon/colour mapping without referencing
/// Microsoft.Maui.Graphics.Color.
/// </summary>
internal static class NotificationIconMap
{
    public static string GetIcon(TestNotificationType type) => type switch
    {
        TestNotificationType.LessonReserved       => "✓",
        TestNotificationType.Waitlist             => "⏱",
        TestNotificationType.NewWorkout           => "★",
        TestNotificationType.LessonCancelled      => "✕",
        TestNotificationType.SubscriptionExpiring => "!",
        TestNotificationType.AttendanceCheckedIn  => "📍",
        _                                         => "•"
    };

    /// <summary>Expected ARGB hex string for each notification type (matches AppNotification.TypeColor).</summary>
    public static string GetColorHex(TestNotificationType type) => type switch
    {
        TestNotificationType.LessonReserved       => "#4CAF50",
        TestNotificationType.Waitlist             => "#FF9800",
        TestNotificationType.NewWorkout           => "#2196F3",
        TestNotificationType.LessonCancelled      => "#F44336",
        TestNotificationType.SubscriptionExpiring => "#FF6F00",
        TestNotificationType.AttendanceCheckedIn  => "#4CAF50",
        _                                         => "#666666"
    };
}
