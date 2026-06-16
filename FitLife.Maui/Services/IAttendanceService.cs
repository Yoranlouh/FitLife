namespace FitLife.Maui.Services;

public enum CheckInMethod
{
    // Manual RFID-badge scan at the gym entrance.
    // TODO production: replace MockAttendanceService with a real NFC/RFID reader integration.
    Rfid,

    // Automatic GPS/geofence detection on arrival.
    // TODO production: replace MockAttendanceService with a real geofence trigger.
    Gps
}

public interface IAttendanceService
{
    bool IsCheckedIn(int lessonId, int userId);
    Task<bool> CheckInAsync(int lessonId, int userId, CheckInMethod method);
}

// MOCK IMPLEMENTATION — stores check-ins in memory only; cleared on app restart.
// Replace the body of CheckInAsync with real RFID/GPS integration for production.
public sealed class MockAttendanceService : IAttendanceService
{
    private readonly HashSet<(int lessonId, int userId)> _checkins = new();

    public bool IsCheckedIn(int lessonId, int userId)
        => _checkins.Contains((lessonId, userId));

    public Task<bool> CheckInAsync(int lessonId, int userId, CheckInMethod method)
    {
        // Simulate a short processing delay (e.g. RFID scan or GPS fix)
        _checkins.Add((lessonId, userId));
        return Task.FromResult(true);
    }
}
