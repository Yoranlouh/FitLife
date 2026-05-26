using FitLife.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

public partial class LessonDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IParticipantService _participantService;
    private readonly IReservationService _reservationService;
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private LessonResponse? _lesson;

    [ObservableProperty]
    private bool _isReserved;

    [ObservableProperty]
    private string _instructorPhone = "06-44221137";

    [ObservableProperty]
    private string _paymentMethod = "1 credit";

    [ObservableProperty]
    private int _participantCount;

    [ObservableProperty]
    private int _maxParticipants;

    [ObservableProperty]
    private bool _isOnWaitlist;

    public LessonDetailViewModel(IParticipantService participantService,
                                 IReservationService reservationService,
                                 IAuthenticationService authenticationService)
    {
        _participantService = participantService;
        _reservationService = reservationService;
        _authenticationService = authenticationService;
        Title = "Groepsevent";
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson = lesson;
            MaxParticipants = lesson.MaxParticipants;

            await LoadParticipantData();
        }
    }

    private async Task LoadParticipantData()
    {
        if (Lesson == null) return;

        IsBusy = true;
        try
        {
            var participants = await _participantService.GetParticipantsAsync(Lesson.Id);
            ParticipantCount = participants.Count();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Reserve()
    {
        if (Lesson == null) return;

        IsReserved = true;
        ParticipantCount++;

        // Notify MyLessonsViewModel so the new reservation appears in "Mijn Lessen".
        WeakReferenceMessenger.Default.Send(new LessonReservedMessage(Lesson));

        await Shell.Current.DisplayAlert("Succes", "Je bent ingeschreven voor deze les.", "OK");
    }

    /// <summary>
    /// Cancels the current reservation on the server, then updates local state
    /// and notifies other ViewModels. Provides clear user feedback on failure.
    /// </summary>
    [RelayCommand]
    private async Task Unregister()
    {
        if (Lesson == null || IsBusy) return;

        // Resolve the current user id; required by the API.
        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert(
                "Niet ingelogd",
                "Je bent niet meer ingelogd. Log opnieuw in om je af te melden.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _reservationService.CancelReservationAsync(Lesson.Id, userId.Value);

            if (result.Success)
            {
                // Only mutate local state after the server confirmed the cancellation.
                IsReserved = false;
                if (ParticipantCount > 0) ParticipantCount--;

                WeakReferenceMessenger.Default.Send(new LessonUnregisteredMessage(Lesson.Id));

                await Shell.Current.DisplayAlert(
                    "Afgemeld",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Je hebt je succesvol afgemeld."
                        : result.Message,
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Afmelden mislukt",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Je kon niet worden afgemeld."
                        : result.Message,
                    "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task JoinWaitlist()
    {
        // Mock wachtlijst
        IsOnWaitlist = true;
        await Shell.Current.DisplayAlert("Wachtlijst", "Je staat nu op de wachtlijst.", "OK");
    }

    [RelayCommand]
    private async Task GoToParticipants()
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "Lesson", Lesson! }
        };
        await Shell.Current.GoToAsync("ParticipantsPage", navigationParameter);
    }
}
