using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

/// <summary>
/// ViewModel for the MyLessonsPage — shows upcoming reservations and attendance history.
/// </summary>
public partial class MyLessonsViewModel : BaseViewModel
{
    private readonly IReservationService    _reservationService;
    private readonly IAuthenticationService _authenticationService;

    // ── Collections ───────────────────────────────────────────────────────
    /// <summary>Upcoming (future) enrolled lessons.</summary>
    public ObservableCollection<UserLesson> EnrolledLessons { get; } = new();

    /// <summary>Past (attended) lessons, newest first.</summary>
    public ObservableCollection<UserLesson> HistoryLessons { get; } = new();

    // ── View toggle ───────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryView))]
    private bool _isMyLessonsView = true;

    public bool IsHistoryView => !IsMyLessonsView;

    // ── History stats ─────────────────────────────────────────────────────
    [ObservableProperty] private int    _totalLessonsAttended;
    [ObservableProperty] private int    _lessonsThisMonth;
    [ObservableProperty] private int    _lessonsThisYear;
    [ObservableProperty] private string _mostVisitedWorkout = "—";

    public MyLessonsViewModel(IReservationService    reservationService,
                              IAuthenticationService authenticationService)
    {
        _reservationService    = reservationService;
        _authenticationService = authenticationService;
        Title = Translator.T("MyLessons_Title");

        // New reservation from LessonDetailPage → add to upcoming list
        WeakReferenceMessenger.Default.Register<LessonReservedMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var lesson = m.Value;
                if (!EnrolledLessons.Any(l => l.Id == lesson.Id))
                    EnrolledLessons.Add(new UserLesson
                    {
                        Id         = lesson.Id,
                        Name       = lesson.WorkoutName,
                        Time       = lesson.StartTime,
                        Instructor = lesson.InstructorName,
                        Location   = lesson.LocationName
                    });
            });
        });

        // Cancellation from LessonDetailPage → remove from upcoming list
        WeakReferenceMessenger.Default.Register<LessonUnregisteredMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var toRemove = EnrolledLessons.FirstOrDefault(l => l.Id == m.Value);
                if (toRemove != null) EnrolledLessons.Remove(toRemove);
            });
        });

    }

    // ── View switch commands ───────────────────────────────────────────────
    [RelayCommand]
    private void SwitchToMyLessons() => IsMyLessonsView = true;

    [RelayCommand]
    private void SwitchToHistory() => IsMyLessonsView = false;

    // ── Data loading ───────────────────────────────────────────────────────
    /// <summary>
    /// Loads all reservations from the API and splits them into upcoming / history.
    /// Safe to call on every OnAppearing.
    /// </summary>
    public async Task LoadMyLessonsAsync()
    {
        if (IsBusy) return;

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0) return;

        try
        {
            IsBusy = true;
            var all = (await _reservationService.GetUserLessonsAsync(userId.Value)).ToList();
            var now = DateTime.Now;

            var upcoming = all.Where(l => l.StartTime >= now)
                              .OrderBy(l => l.StartTime)
                              .ToList();

            var history  = all.Where(l => l.StartTime < now)
                              .OrderByDescending(l => l.StartTime)
                              .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Upcoming
                EnrolledLessons.Clear();
                foreach (var l in upcoming)
                    EnrolledLessons.Add(Map(l));

                // History
                HistoryLessons.Clear();
                foreach (var l in history)
                    HistoryLessons.Add(Map(l));

                // Stats
                TotalLessonsAttended = history.Count;
                LessonsThisMonth     = history.Count(l => l.StartTime.Year  == now.Year &&
                                                          l.StartTime.Month == now.Month);
                LessonsThisYear      = history.Count(l => l.StartTime.Year  == now.Year);
                MostVisitedWorkout   = history
                    .GroupBy(l => l.WorkoutName)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "—";
            });
        }
        finally { IsBusy = false; }
    }

    private static UserLesson Map(UserLessonDto d) => new()
    {
        Id         = d.Id,
        Name       = d.WorkoutName,
        Time       = d.StartTime,
        Instructor = d.InstructorName,
        Location   = d.LocationName
    };

    /// <summary>
    /// Cancels the given reservation server-side via the FitLife.API.
    /// Shows a confirmation dialog, calls the API, only mutates the local list
    /// after a successful response, and notifies other parts of the app via messaging.
    /// </summary>
    /// <param name="lesson">The lesson to cancel.</param>
    [RelayCommand]
    private async Task CancelReservation(UserLesson lesson)
    {
        if (lesson == null || IsBusy) return;

        // Ask the user to confirm before doing anything destructive.
        bool confirm = await Shell.Current.DisplayAlert(
            Translator.T("MyLessons_ConfirmTitle"),
            Translator.T("MyLessons_ConfirmBody", lesson.Name),
            Translator.T("Common_Yes"),
            Translator.T("Common_No"));

        if (!confirm) return;

        try
        {
            IsBusy = true;

            var userId = _authenticationService.CurrentUserId;
            if (userId is null or <= 0)
            {
                await Shell.Current.DisplayAlert(
                    Translator.T("Common_NotLoggedIn"),
                    Translator.T("MyLessons_LoginToCancel"),
                    Translator.T("Common_OK"));
                return;
            }

            // Perform the actual server-side cancellation.
            var result = await _reservationService.CancelReservationAsync(lesson.Id, userId.Value);

            if (result.Success)
            {
                // Only mutate the UI list AFTER the server confirmed the cancellation.
                EnrolledLessons.Remove(lesson);

                // Notify other ViewModels (e.g. LessonDetailViewModel) so they can refresh.
                WeakReferenceMessenger.Default.Send(new LessonUnregisteredMessage(lesson.Id));

                await Shell.Current.DisplayAlert(
                    Translator.T("MyLessons_CancelledTitle"),
                    string.IsNullOrWhiteSpace(result.Message)
                        ? Translator.T("MyLessons_CancelledBody")
                        : result.Message,
                    Translator.T("Common_OK"));
            }
            else
            {
                // Show the server-provided error message (e.g. "geen actieve reservering").
                await Shell.Current.DisplayAlert(
                    Translator.T("MyLessons_CancelFailedTitle"),
                    string.IsNullOrWhiteSpace(result.Message)
                        ? Translator.T("MyLessons_CancelFailedBody")
                        : result.Message,
                    Translator.T("Common_OK"));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Simplified model for representing an enrolled lesson in the UI.
/// </summary>
public class UserLesson
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Instructor { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Message sent when a user successfully reserves a lesson.
/// </summary>
public class LessonReservedMessage : ValueChangedMessage<LessonResponse>
{
    public LessonReservedMessage(LessonResponse value) : base(value)
    {
    }
}

/// <summary>
/// Message sent when a user unregisters from a lesson.
/// </summary>
public class LessonUnregisteredMessage : ValueChangedMessage<int>
{
    public LessonUnregisteredMessage(int lessonId) : base(lessonId)
    {
    }
}
