using FitLife.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
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
    private readonly IParticipantService      _participantService;
    private readonly IReservationService      _reservationService;
    private readonly IAuthenticationService   _authenticationService;
    private readonly INotificationService     _notificationService;
    private readonly IAttendanceService       _attendanceService;
    private readonly IBikeReservationService  _bikeReservationService;

    // The lesson being shown — populated from the Shell navigation query
    [ObservableProperty]
    private LessonResponse? _lesson;

    // Whether the current user has an active reservation for this lesson.
    // NotifyPropertyChangedFor regenerates ShowReserveButton and ShowCancellationDeadlineWarning
    // whenever this value changes.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowReserveButton))]
    [NotifyPropertyChangedFor(nameof(ShowCancellationDeadlineWarning))]
    [NotifyPropertyChangedFor(nameof(ShowCheckInSection))]
    [NotifyPropertyChangedFor(nameof(ShowCheckInButtons))]
    [NotifyPropertyChangedFor(nameof(ShowGpsCheckInButton))]
    [NotifyPropertyChangedFor(nameof(ShowCheckInEarlyMessage))]
    [NotifyPropertyChangedFor(nameof(ShowLessonEndedMessage))]
    [NotifyPropertyChangedFor(nameof(ShowBikeSection))]
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

    // ── Attendance ──────────────────────────────────────────────────────────

    // True when the current user has checked in (RFID or GPS) for this lesson
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCheckedInBadge))]
    [NotifyPropertyChangedFor(nameof(ShowCheckInButtons))]
    [NotifyPropertyChangedFor(nameof(ShowGpsCheckInButton))]
    [NotifyPropertyChangedFor(nameof(ShowCheckInEarlyMessage))]
    [NotifyPropertyChangedFor(nameof(ShowLessonEndedMessage))]
    private bool _isCheckedIn;

    // True when the check-in window is open: 15 min before start until lesson ends
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCheckInButtons))]
    [NotifyPropertyChangedFor(nameof(ShowGpsCheckInButton))]
    [NotifyPropertyChangedFor(nameof(ShowCheckInEarlyMessage))]
    private bool _isCheckInWindowOpen;

    // True when the lesson has fully ended (now > EndTime)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCheckInEarlyMessage))]
    [NotifyPropertyChangedFor(nameof(ShowLessonEndedMessage))]
    private bool _isLessonEnded;

    // ── Spinning bike selection ──────────────────────────────────────────────────

    // True when the current lesson's workout name is "Spinning" (case-insensitive)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBikeSection))]
    private bool _isSpinningLesson;

    // Shows the full label of the selected bike, e.g. "Jouw fiets: Rij 2 - Fiets 3"
    [ObservableProperty]
    private string _selectedBikeDisplay = string.Empty;

    // True after the user has successfully selected a bike
    [ObservableProperty]
    private bool _hasBikeSelected;

    // 4×4 grid of all bikes for the spinning lesson
    public ObservableCollection<BikeItem> BikeOptions { get; } = new();

    // The bike section is shown only when reserved for a spinning lesson
    public bool ShowBikeSection => IsSpinningLesson && IsReserved;

    // ── Attendance ──────────────────────────────────────────────────────────────

    // Shown when check-in window is not yet open, e.g. "Beschikbaar vanaf 09:45"
    [ObservableProperty]
    private string _checkInEarlyText = "";

    // Reads the location permission setting saved by SettingsViewModel
    public bool IsLocationSharingEnabled
        => Preferences.Get("settings_location_sharing", false);

    // Show the entire attendance card whenever the user is reserved
    public bool ShowCheckInSection => IsReserved;

    // Green "aanwezig" badge — only after a successful check-in
    public bool ShowCheckedInBadge => IsCheckedIn;

    // RFID check-in button: reserved, not yet checked in, window is open
    public bool ShowCheckInButtons => IsReserved && !IsCheckedIn && IsCheckInWindowOpen;

    // GPS simulate button: same as RFID, plus location permission enabled
    public bool ShowGpsCheckInButton => IsReserved && !IsCheckedIn && IsCheckInWindowOpen && IsLocationSharingEnabled;

    // "Too early" notice: reserved, not checked in, window not yet open, lesson not ended
    public bool ShowCheckInEarlyMessage => IsReserved && !IsCheckedIn && !IsCheckInWindowOpen && !IsLessonEnded;

    // "Lesson ended without check-in" notice
    public bool ShowLessonEndedMessage => IsReserved && !IsCheckedIn && IsLessonEnded;

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
            if (IsAdvanced) return Translator.T("Common_Unlimited");
            return CurrentCredits.HasValue ? Translator.T("Detail_CreditsLeft", CurrentCredits.Value) : "—";
        }
    }

    // Shows the subscription tier and cost model, e.g. "Rookie · 1 credit per les"
    public string SubscriptionLineDisplay
        => IsAdvanced
            ? Translator.T("Detail_SubLineUnlimited", SubscriptionName)
            : Translator.T("Detail_SubLineCredit", SubscriptionName);

    // Advanced subscribers have unlimited lessons (identified by name or a credit value ≥ 999)
    private bool IsAdvanced
        => string.Equals(SubscriptionName, "Advanced", StringComparison.OrdinalIgnoreCase)
           || CurrentCredits >= 999;

    public LessonDetailViewModel(IParticipantService     participantService,
                                 IReservationService     reservationService,
                                 IAuthenticationService  authenticationService,
                                 INotificationService    notificationService,
                                 IAttendanceService      attendanceService,
                                 IBikeReservationService bikeReservationService)
    {
        _participantService     = participantService;
        _reservationService     = reservationService;
        _authenticationService  = authenticationService;
        _notificationService    = notificationService;
        _attendanceService      = attendanceService;
        _bikeReservationService = bikeReservationService;
        Title = Translator.T("Detail_PageTitle");
    }

    // Receives the LessonResponse from the previous page via Shell query.
    // Sets booking rules (too far / cancellation deadline) and kicks off async data loads.
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Lesson", out var lessonObj) && lessonObj is LessonResponse lesson)
        {
            Lesson           = lesson;
            MaxParticipants  = lesson.MaxParticipants;
            IsReserved       = lesson.IsBooked;
            IsSpinningLesson = lesson.WorkoutName.Equals("Spinning", StringComparison.OrdinalIgnoreCase);

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
                ReservationOpenDateText = Translator.T("Detail_BookableFrom", openDate);
            }

            // ── Attendance state ─────────────────────────────────────────────
            var checkInOpensAt = lesson.StartTime.AddMinutes(-15);
            IsCheckInWindowOpen = now >= checkInOpensAt && now <= lesson.EndTime;
            IsLessonEnded       = now > lesson.EndTime;

            if (!IsCheckInWindowOpen && !IsLessonEnded)
                CheckInEarlyText = Translator.T("Attendance_NotYetOpen", checkInOpensAt);

            var userId = _authenticationService.CurrentUserId;
            if (userId.HasValue)
                IsCheckedIn = _attendanceService.IsCheckedIn(lesson.Id, userId.Value);

            LoadSubscriptionData();        // read from in-memory auth service (instant)
            await LoadParticipantData();   // async API call

            // Load bike grid for spinning lessons where the user is already registered
            if (IsSpinningLesson && IsReserved)
                await LoadBikesAsync();
        }
    }

    // Reads the current user's subscription info from the authentication service
    // and populates the credits / subscription display properties.
    private void LoadSubscriptionData()
    {
        SubscriptionName = _authenticationService.CurrentUserSubscriptionType ?? Translator.T("Common_Unknown");
        CurrentCredits   = _authenticationService.CurrentUserCredits;

        if (!string.IsNullOrEmpty(_authenticationService.CurrentUserSubscriptionRenewalDate)
            && DateTime.TryParse(_authenticationService.CurrentUserSubscriptionRenewalDate, out var renewal))
        {
            RenewalDateDisplay = Translator.T("Detail_Expires", renewal);
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
                Translator.T("Detail_NotPossibleTitle"),
                Translator.T("Detail_TooFarBody", ReservationOpenDateText),
                Translator.T("Common_OK"));
            return;
        }

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_NotLoggedIn"), Translator.T("Detail_LoginToReserve"), Translator.T("Common_OK"));
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

                // For spinning lessons, load the bike grid so the member can pick a seat
                if (IsSpinningLesson)
                    await LoadBikesAsync();

                // Notify MyLessonsViewModel so it can add this lesson to the upcoming list
                WeakReferenceMessenger.Default.Send(new LessonReservedMessage(Lesson));

                _notificationService.Add(
                    userId.Value,
                    Translator.T("Notif_ReservedTitle"),
                    Translator.T("Notif_ReservedBody", Lesson.WorkoutName, Lesson.StartTime),
                    NotificationType.LessonReserved);

                var msg = string.IsNullOrWhiteSpace(result.Message)
                    ? Translator.T("Detail_ReservedDefault")
                    : result.Message;
                await Shell.Current.DisplayAlert(Translator.T("Detail_ReservedAlertTitle"), msg, Translator.T("Common_OK"));
            }
            else if (result.LessonFull)
            {
                // Lesson is at capacity — automatically add the user to the waitlist
                var waitlistResult = await _reservationService.JoinWaitlistAsync(Lesson.Id, userId.Value);

                if (waitlistResult.Success)
                {
                    IsOnWaitlist = true;

                    _notificationService.Add(
                        userId.Value,
                        Translator.T("Notif_WaitlistTitle"),
                        Translator.T("Notif_WaitlistBody", Lesson.WorkoutName, Lesson.StartTime),
                        NotificationType.Waitlist);

                    await Shell.Current.DisplayAlert(
                        Translator.T("Detail_LessonFullWaitlistTitle"),
                        Translator.T("Detail_LessonFullWaitlistBody"),
                        Translator.T("Common_OK"));
                }
                else if (waitlistResult.AlreadyOnWaitlist)
                {
                    await Shell.Current.DisplayAlert(
                        Translator.T("Detail_LessonFullWaitlistTitle"),
                        Translator.T("Detail_AlreadyOnWaitlist"),
                        Translator.T("Common_OK"));
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        Translator.T("Detail_WaitlistFailedTitle"),
                        Translator.T("Detail_WaitlistFailedBody"),
                        Translator.T("Common_OK"));
                }
            }
            else
            {
                await Shell.Current.DisplayAlert(Translator.T("Detail_ReserveFailedTitle"),
                    string.IsNullOrWhiteSpace(result.Message)
                        ? Translator.T("Detail_ReserveFailedBody")
                        : result.Message,
                    Translator.T("Common_OK"));
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
                Translator.T("Detail_CancelNotPossibleTitle"),
                Translator.T("Detail_CancelDeadlineBody"),
                Translator.T("Common_OK"));
            return;
        }

        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_NotLoggedIn"),
                Translator.T("Detail_LoginToCancel"), Translator.T("Common_OK"));
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

                // The cancel endpoint already releases the bike server-side — just clear local state
                if (IsSpinningLesson)
                {
                    BikeOptions.Clear();
                    SelectedBikeDisplay = string.Empty;
                    HasBikeSelected     = false;
                }

                // Notify MyLessonsViewModel to remove this lesson from the upcoming list
                WeakReferenceMessenger.Default.Send(new LessonUnregisteredMessage(Lesson.Id));

                await Shell.Current.DisplayAlert(Translator.T("Detail_UnregisteredTitle"),
                    string.IsNullOrWhiteSpace(result.Message)
                        ? Translator.T("Detail_UnregisteredBody")
                        : result.Message,
                    Translator.T("Common_OK"));
            }
            else
            {
                await Shell.Current.DisplayAlert(Translator.T("Detail_CancelFailedTitle"),
                    string.IsNullOrWhiteSpace(result.Message)
                        ? Translator.T("Detail_CancelFailedBody")
                        : result.Message,
                    Translator.T("Common_OK"));
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
            await Shell.Current.DisplayAlert(Translator.T("Common_NotLoggedIn"), Translator.T("Common_LoginAgain"), Translator.T("Common_OK"));
            return;
        }

        IsOnWaitlist = true;

        // Only members receive a waitlist notification.
        if (_authenticationService.IsMember)
        {
            _notificationService.Add(
                userId.Value,
                Translator.T("Notif_WaitlistTitle"),
                Translator.T("Notif_WaitlistBody", Lesson.WorkoutName, Lesson.StartTime),
                NotificationType.Waitlist);
        }

        await Shell.Current.DisplayAlert(Translator.T("Participants_Waitlist"), Translator.T("Detail_OnWaitlistNow"), Translator.T("Common_OK"));
    }

    // Manual RFID check-in — the user taps the button to simulate scanning their badge.
    // TODO production: trigger actual NFC/RFID read here instead of calling CheckInAsync directly.
    [RelayCommand]
    private async Task CheckInRfid()
    {
        if (Lesson == null || IsBusy) return;
        await PerformCheckIn(CheckInMethod.Rfid);
    }

    // GPS-simulated check-in — available only when location sharing is enabled in Settings.
    // TODO production: replace with a real geofence trigger; remove the manual button.
    [RelayCommand]
    private async Task CheckInGps()
    {
        if (Lesson == null || IsBusy) return;
        await PerformCheckIn(CheckInMethod.Gps);
    }

    private async Task PerformCheckIn(CheckInMethod method)
    {
        var userId = _authenticationService.CurrentUserId;
        if (userId is null or <= 0)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_NotLoggedIn"), Translator.T("Common_LoginAgain"), Translator.T("Common_OK"));
            return;
        }

        IsBusy = true;
        try
        {
            var success = await _attendanceService.CheckInAsync(Lesson!.Id, userId.Value, method);
            if (!success) return;

            IsCheckedIn = true;

            _notificationService.Add(
                userId.Value,
                Translator.T("Notif_CheckedInTitle"),
                Translator.T("Notif_CheckedInBody", Lesson.WorkoutName, DateTime.Now),
                NotificationType.AttendanceCheckedIn);

            var title = method == CheckInMethod.Gps
                ? Translator.T("Attendance_GpsAlertTitle")
                : Translator.T("Attendance_AlertTitle");
            var body = method == CheckInMethod.Gps
                ? Translator.T("Attendance_GpsAlertBody")
                : Translator.T("Attendance_AlertBody", Lesson.WorkoutName);

            await Shell.Current.DisplayAlert(title, body, Translator.T("Common_OK"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Fetches the 16-bike grid from the API and updates BikeOptions + SelectedBikeDisplay.
    // Called after reservation succeeds and when navigating to a spinning lesson already booked.
    private async Task LoadBikesAsync()
    {
        var userId = _authenticationService.CurrentUserId;
        if (Lesson == null || userId == null) return;

        var bikes = await _bikeReservationService.GetBikesAsync(Lesson.Id, userId.Value);

        BikeOptions.Clear();
        foreach (var bike in bikes)
            BikeOptions.Add(bike);

        var own = bikes.FirstOrDefault(b => b.IsSelectedByCurrentUser);
        if (own != null)
        {
            SelectedBikeDisplay = Translator.T("Spinning_YourBike",
                Translator.T("Spinning_BikeLabel", own.RowNumber, own.BikeNumber));
            HasBikeSelected = true;
        }
        else
        {
            SelectedBikeDisplay = string.Empty;
            HasBikeSelected     = false;
        }
    }

    // Called when the user taps a bike button in the 4×4 grid.
    // Available bikes can be selected or switched; taken bikes are disabled in the UI.
    [RelayCommand]
    private async Task SelectBike(BikeItem bike)
    {
        if (Lesson == null || IsBusy || !bike.IsAvailable) return;

        var userId = _authenticationService.CurrentUserId;
        if (userId == null) return;

        IsBusy = true;
        try
        {
            var result = await _bikeReservationService.SelectBikeAsync(
                Lesson.Id, userId.Value, bike.RowNumber, bike.BikeNumber);

            if (result.Success)
                await LoadBikesAsync(); // refresh grid to reflect the new selection
            else
                await Shell.Current.DisplayAlert(
                    Translator.T("Common_Error"), result.Message, Translator.T("Common_OK"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Navigates to the full participant list page for this lesson.
    [RelayCommand]
    private async Task GoToParticipants()
    {
        var navigationParameter = new Dictionary<string, object> { { "Lesson", Lesson! } };
        await Shell.Current.GoToAsync("ParticipantsPage", navigationParameter);
    }
}
