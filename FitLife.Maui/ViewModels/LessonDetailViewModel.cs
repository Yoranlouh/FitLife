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
    private int _participantCount;

    [ObservableProperty]
    private int _maxParticipants;

    [ObservableProperty]
    private bool _isOnWaitlist;

    // ── Subscription & credits (real data from auth service) ──────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditsDisplay))]
    [NotifyPropertyChangedFor(nameof(SubscriptionLineDisplay))]
    private int? _currentCredits;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditsDisplay))]
    [NotifyPropertyChangedFor(nameof(SubscriptionLineDisplay))]
    private string _subscriptionName = string.Empty;

    [ObservableProperty]
    private string _renewalDateDisplay = string.Empty;

    /// <summary>e.g. "Onbeperkt" or "4 credits over"</summary>
    public string CreditsDisplay
    {
        get
        {
            if (IsAdvanced) return "Onbeperkt";
            return CurrentCredits.HasValue ? $"{CurrentCredits.Value} credits over" : "—";
        }
    }

    /// <summary>e.g. "Advanced abonnement" or "Rookie · 1 credit per les"</summary>
    public string SubscriptionLineDisplay
        => IsAdvanced
            ? $"{SubscriptionName} · Onbeperkt lessen"
            : $"{SubscriptionName} · 1 credit per les";

    private bool IsAdvanced
        => string.Equals(SubscriptionName, "Advanced", StringComparison.OrdinalIgnoreCase)
           || CurrentCredits >= 999;

    public LessonDetailViewModel(IParticipantService participantService,
                                 IReservationService reservationService,
                                 IAuthenticationService authenticationService)
    {
        _participantService    = participantService;
        _reservationService    = reservationService;
        _authenticationService = authenticationService;
        Title = "Groepsevent";
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson          = lesson;
            MaxParticipants = lesson.MaxParticipants;

            LoadSubscriptionData();
            await LoadParticipantData();
        }
    }

    private void LoadSubscriptionData()
    {
        SubscriptionName = _authenticationService.CurrentUserSubscriptionType ?? "Onbekend";
        CurrentCredits   = _authenticationService.CurrentUserCredits;

        if (!string.IsNullOrEmpty(_authenticationService.CurrentUserSubscriptionRenewalDate)
            && DateTime.TryParse(_authenticationService.CurrentUserSubscriptionRenewalDate, out var renewal))
        {
            RenewalDateDisplay = $"Verloopt: {renewal:d MMMM yyyy}";
        }
        else
        {
            RenewalDateDisplay = string.Empty;
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
        if (Lesson == null || IsBusy) return;

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert(
                "Niet ingelogd",
                "Log opnieuw in om een les te reserveren.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _reservationService.ReserveAsync(Lesson.Id, userId.Value);

            if (result.Success)
            {
                IsReserved = true;
                ParticipantCount++;

                // Update credits immediately (-1), unless advanced/unlimited
                if (!IsAdvanced && result.RemainingCredits.HasValue)
                    CurrentCredits = result.RemainingCredits.Value;
                else if (!IsAdvanced && CurrentCredits.HasValue)
                    CurrentCredits = CurrentCredits.Value - 1;

                WeakReferenceMessenger.Default.Send(new LessonReservedMessage(Lesson));

                var msg = string.IsNullOrWhiteSpace(result.Message)
                    ? "Je bent ingeschreven voor deze les."
                    : result.Message;
                await Shell.Current.DisplayAlert("Ingeschreven!", msg, "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Inschrijven mislukt",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Je kon niet worden ingeschreven."
                        : result.Message,
                    "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
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
                IsReserved = false;
                if (ParticipantCount > 0) ParticipantCount--;

                // Refund 1 credit on cancellation, unless advanced/unlimited
                if (!IsAdvanced && result.RemainingCredits.HasValue)
                    CurrentCredits = result.RemainingCredits.Value;
                else if (!IsAdvanced && CurrentCredits.HasValue)
                    CurrentCredits = CurrentCredits.Value + 1;

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
