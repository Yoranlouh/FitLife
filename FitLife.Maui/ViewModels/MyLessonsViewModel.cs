using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

/// <summary>
/// ViewModel for the MyLessonsPage, responsible for displaying lessons the user is enrolled in.
/// Listens for messages from other ViewModels to update the list in real-time and performs
/// reservation cancellations against the FitLife.API.
/// </summary>
public partial class MyLessonsViewModel : BaseViewModel
{
    // Server-side reservation service (HTTP).
    private readonly IReservationService _reservationService;

    // Authentication service used to retrieve the current user's id,
    // which the API needs to identify whose reservation must be cancelled.
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// Collection of lessons the user is currently enrolled in.
    /// </summary>
    public ObservableCollection<UserLesson> EnrolledLessons { get; } = new();

    public MyLessonsViewModel(IReservationService reservationService,
                              IAuthenticationService authenticationService)
    {
        _reservationService = reservationService;
        _authenticationService = authenticationService;

        Title = "Mijn Lessen";

        // Listen for new reservations from LessonDetailPage.
        WeakReferenceMessenger.Default.Register<LessonReservedMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var lesson = m.Value;
                // Avoid duplicates.
                if (!EnrolledLessons.Any(l => l.Id == lesson.Id))
                {
                    EnrolledLessons.Add(new UserLesson
                    {
                        Id = lesson.Id,
                        Name = lesson.WorkoutName,
                        Time = lesson.StartTime,
                        Instructor = lesson.InstructorName,
                        Location = lesson.LocationName
                    });
                }
            });
        });

        // Listen for unregistrations from LessonDetailPage so the list stays in sync.
        WeakReferenceMessenger.Default.Register<LessonUnregisteredMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var lessonId = m.Value;
                var lessonToRemove = EnrolledLessons.FirstOrDefault(l => l.Id == lessonId);
                if (lessonToRemove != null)
                {
                    EnrolledLessons.Remove(lessonToRemove);
                }
            });
        });

        LoadMyLessons();
    }

    /// <summary>
    /// Loads the initial list of lessons. Currently uses mock data with negative ids
    /// so the cancel-flow can distinguish them from real, server-known reservations.
    /// </summary>
    private void LoadMyLessons()
    {
        // Mock data for demonstration purposes (negative ids => not present in DB).
        EnrolledLessons.Add(new UserLesson { Id = -1, Name = "Spinning", Time = DateTime.Today.AddHours(18), Instructor = "Marco", Location = "Zaal 1" });
        EnrolledLessons.Add(new UserLesson { Id = -2, Name = "Yoga", Time = DateTime.Today.AddDays(2).AddHours(10), Instructor = "Sarah", Location = "Zaal 3" });
    }

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
            "Bevestigen",
            $"Weet je zeker dat je de reservering voor {lesson.Name} wilt annuleren?",
            "Ja",
            "Nee");

        if (!confirm) return;

        try
        {
            IsBusy = true;

            // Demo/mock entries (negative ids) do not exist in the database.
            // We remove them client-side only to keep the demo working.
            if (lesson.Id < 0)
            {
                EnrolledLessons.Remove(lesson);
                WeakReferenceMessenger.Default.Send(new LessonUnregisteredMessage(lesson.Id));
                await Shell.Current.DisplayAlert("Geannuleerd", "Je reservering is geannuleerd.", "OK");
                return;
            }

            // We need the currently authenticated user's id to ask the API
            // to cancel the right reservation.
            var userId = _authenticationService.CurrentUserId;
            if (userId is null or <= 0)
            {
                await Shell.Current.DisplayAlert(
                    "Niet ingelogd",
                    "Je bent niet meer ingelogd. Log opnieuw in om je reservering te annuleren.",
                    "OK");
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
                    "Geannuleerd",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Je reservering is geannuleerd."
                        : result.Message,
                    "OK");
            }
            else
            {
                // Show the server-provided error message (e.g. "geen actieve reservering").
                await Shell.Current.DisplayAlert(
                    "Annuleren mislukt",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "De reservering kon niet worden geannuleerd."
                        : result.Message,
                    "OK");
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
