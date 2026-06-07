using FitLife.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

// ViewModel for the lesson detail page.
// Implements IQueryAttributable so the full LessonResponse object can be passed
// via Shell navigation (instead of just an ID) to avoid a second API call.
public partial class LessonDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IParticipantService     _participantService;
    private readonly IReservationService     _reservationService;
    private readonly IAuthenticationService  _authenticationService;
    private readonly INotificationService    _notificationService;

    // The lesson being shown — populated from the Shell navigation query
    [ObservableProperty]
    private LessonResponse? _lesson;

    // Whether the current user has an active reservation for this lesson.
    // NotifyPropertyChangedFor regenerates ShowReserveButton and ShowCancellationDeadlineWarning
    // whenever this value changes.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowReserveButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancellationDeadlineWarning))]
    private bool _isReserved;

    // True when the lesson is more than 7 days away (booking window not yet open)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowReserveButton))]
    private bool _isTooFarInFuture;

    // True when the lesson has already started or is in the past
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowReserveButton))]
    [NotifyPropertyChangedFor(nameof(ShowExpiredMessage))]
    private bool _isLessonStartedOrPast;

    // True when the lesson starts within 1 hour (cancellation window has closed)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCancellationDeadlineWarning))]
    private bool _isCancellationDeadlinePassed;

    // Displayed when the booking window is not yet open, e.g. "Reserveren mogelijk vanaf 15 juni"
    [ObservableProperty]
    private string _reservationOpenDateText = "";

    // The "Reserve" button is only shown when not already reserved, booking window is open, and lesson hasn't started
    public bool ShowReserveButton => !IsReserved && !IsTooFarInFuture && !IsLessonStartedOrPast;

    // The "Aanmelden niet meer mogelijk" message is shown when the lesson is past/ongoing and user is not registered
    public bool ShowExpiredMessage => !IsReserved && IsLessonStartedOrPast;

    // A warning banner is shown when the cancellation window has passed
    public bool ShowCancellationDeadlineWarning => IsReserved && IsCancellationDeadlinePassed;

    [ObservableProperty] private int  _participantCount;
    [ObservableProperty] private int  _maxParticipants;
    [ObservableProperty] private bool _isOnWaitlist;

    // Subscription data read from the auth service — displayed in the info card
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

    // Shows "Onbeperkt" for Advanced subscribers, otherwise the remaining credit count
    public string CreditsDisplay
    {
        get
        {
            if (IsAdvanced) return "Onbeperkt";
            return CurrentCredits.HasValue ? $"{CurrentCredits.Value} credits over" : "—";
        }
    }

    // Shows the subscription tier and cost model, e.g. "Rookie · 1 credit per les"
    public string SubscriptionLineDisplay
        => IsAdvanced
            ? $"{SubscriptionName} · Onbeperkt lessen"
            : $"{SubscriptionName} · 1 credit per les";

    // Advanced subscribers have unlimited lessons (identified by name or a credit value ≥ 999)
    private bool IsAdvanced
        => string.Equals(SubscriptionName, "Advanced", StringComparison.OrdinalIgnoreCase)
           || CurrentCredits >= 999;

    public LessonDetailViewModel(IParticipantService    participantService,
                                 IReservationService    reservationService,
                                 IAuthenticationService authenticationService,
                                 INotificationService   notificationService)
    {
        _participantService    = participantService;
        _reservationService    = reservationService;
        _authenticationService = authenticationService;
        _notificationService   = notificationService;
        Title = "Groepsevent";
    }

    // Receives the LessonResponse from the previous page via Shell query.
    // Sets booking rules (too far / cancellation deadline) and kicks off async data loads.
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson          = lesson;
            MaxParticipants = lesson.MaxParticipants;

            var now = DateTime.Now;
            // Booking opens 7 days before the lesson
            IsTooFarInFuture             = lesson.StartTime > now.AddDays(7);
            // Lesson has already started or is in the past — booking is no longer allowed
            IsLessonStartedOrPast        = lesson.StartTime <= now;
            // Cancellation must be done at least 1 hour before start
            IsCancellationDeadlinePassed = lesson.StartTime <= now.AddHours(1);

            if (IsTooFarInFuture)
            {
                var openDate = lesson.StartTime.AddDays(-7);
                ReservationOpenDateText = $"Reserveren mogelijk vanaf {openDate:d MMMM}";
            }

            LoadSubscriptionData();        // read from in-memory auth service (instant)
            await LoadParticipantData();   // async API call
        }
    }

    // Reads the current user's subscription info from the authentication service
    // and populates the credits / subscription display properties.
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

    // Fetches the current participant count from GET /lessons/{id}/participants.
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

    // Reserves the lesson for the current user via POST /lessons/{id}/reserve.
    // Deducts 1 credit (unless Advanced), updates local state optimistically,
    // sends a WeakReferenceMessenger message so MyLessonsPage can refresh its list,
    // and creates an in-app notification.
    [RelayCommand]
    private async Task Reserve()
    {
        if (Lesson == null || IsBusy) return;

        // Enforce the 7-day booking window rule before calling the API
        if (IsTooFarInFuture)
        {
            await Shell.Current.DisplayAlert(
                "Niet mogelijk",
                $"Je kunt maximaal 1 week van tevoren reserveren.\n{ReservationOpenDateText}.",
                "OK");
            return;
        }

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert("Niet ingelogd", "Log opnieuw in om een les te reserveren.", "OK");
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

                // Update the credit counter shown on this page — use the server value if available
                if (!IsAdvanced && result.RemainingCredits.HasValue)
                    CurrentCredits = result.RemainingCredits.Value;
                else if (!IsAdvanced && CurrentCredits.HasValue)
                    CurrentCredits = CurrentCredits.Value - 1;

                // Notify MyLessonsViewModel so it can add this lesson to the upcoming list
                WeakReferenceMessenger.Default.Send(new LessonReservedMessage(Lesson));

                _notificationService.Add(
                    userId.Value,
                    "Aangemeld voor les",
                    $"Je bent ingeschreven voor {Lesson.WorkoutName} op {Lesson.StartTime:d MMMM 'om' HH:mm}.",
                    NotificationType.LessonReserved);

                var msg = string.IsNullOrWhiteSpace(result.Message)
                    ? "Je bent ingeschreven voor deze les."
                    : result.Message;
                await Shell.Current.DisplayAlert("Ingeschreven!", msg, "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Inschrijven mislukt",
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

    // Cancels the current reservation via DELETE /lessons/{id}/cancel.
    // Enforces the 1-hour cancellation deadline, refunds 1 credit on success,
    // and notifies other ViewModels via WeakReferenceMessenger.
    [RelayCommand]
    private async Task Unregister()
    {
        if (Lesson == null || IsBusy) return;

        if (IsCancellationDeadlinePassed)
        {
            await Shell.Current.DisplayAlert(
                "Afmelden niet mogelijk",
                "Je kunt je niet meer afmelden. Afmelden is alleen mogelijk tot 1 uur voor aanvang van de les.",
                "OK");
            return;
        }

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert("Niet ingelogd",
                "Je bent niet meer ingelogd. Log opnieuw in om je af te melden.", "OK");
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

                // Refund the credit that was deducted at reservation time
                if (!IsAdvanced && result.RemainingCredits.HasValue)
                    CurrentCredits = result.RemainingCredits.Value;
                else if (!IsAdvanced && CurrentCredits.HasValue)
                    CurrentCredits = CurrentCredits.Value + 1;

                // Notify MyLessonsViewModel to remove this lesson from the upcoming list
                WeakReferenceMessenger.Default.Send(new LessonUnregisteredMessage(Lesson.Id));

                await Shell.Current.DisplayAlert("Afgemeld",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Je hebt je succesvol afgemeld."
                        : result.Message,
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Afmelden mislukt",
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

    // Registers the user on the waitlist (client-side only for now) and
    // creates a notification so the user knows their position.
    [RelayCommand]
    private async Task JoinWaitlist()
    {
        if (Lesson == null) return;

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert("Niet ingelogd", "Log opnieuw in.", "OK");
            return;
        }

        IsOnWaitlist = true;

        // Only members receive a waitlist notification.
        if (_authenticationService.IsMember)
        {
            _notificationService.Add(
                userId.Value,
                "Op de wachtlijst",
                $"Je staat op de wachtlijst voor {Lesson.WorkoutName} op {Lesson.StartTime:d MMMM 'om' HH:mm}. We laten je weten als er een plek vrijkomt.",
                NotificationType.Waitlist);
        }

        await Shell.Current.DisplayAlert("Wachtlijst", "Je staat nu op de wachtlijst.", "OK");
    }

    // Navigates to the full participant list page for this lesson.
    [RelayCommand]
    private async Task GoToParticipants()
    {
        var navigationParameter = new Dictionary<string, object> { { "Lesson", Lesson! } };
        await Shell.Current.GoToAsync("ParticipantsPage", navigationParameter);
    }
}
