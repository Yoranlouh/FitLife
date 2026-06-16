using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using Microsoft.Maui.Storage;

namespace FitLife.Maui.ViewModels;

/// <summary>
/// ViewModel for the Subscription Management page
/// Handles displaying available subscription plans and managing subscription changes
/// </summary>
public partial class SubscriptionViewModel : BaseViewModel
{
    private readonly ISubscriptionService   _subscriptionService;
    private readonly IAuthenticationService _authService;
    private readonly INotificationService   _notificationService;

    // Current subscription information
    [ObservableProperty]
    private string _currentSubscription = "Laden...";

    [ObservableProperty]
    private DateTime? _expiryDate = null;

    [ObservableProperty]
    private string _expiryDateDisplay = "Laden...";

    // Pending subscription change info
    [ObservableProperty]
    private bool _hasPendingChange = false;

    [ObservableProperty]
    private string _pendingChangeMessage = string.Empty;

    // Yearly/Monthly toggle
    [ObservableProperty]
    private bool _isYearly = false;

    // Available subscription plans loaded from API
    public ObservableCollection<SubscriptionOption> AvailableSubscriptions { get; } = new();

    public SubscriptionViewModel(ISubscriptionService subscriptionService,
                                 IAuthenticationService authService,
                                 INotificationService notificationService)
    {
        _subscriptionService = subscriptionService;
        _authService         = authService;
        _notificationService = notificationService;
        Title = Translator.T("Profile_ManageSubscription");
    }

    /// <summary>
    /// Called when page appears - loads current subscription status and available plans
    /// </summary>
    public async Task LoadDataAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SubscriptionViewModel] LoadDataAsync called");

        if (!_authService.IsAuthenticated || _authService.CurrentUserId == null)
        {
            System.Diagnostics.Debug.WriteLine("[SubscriptionViewModel] User not authenticated");
            CurrentSubscription = Translator.T("Subscription_NotLoggedInTitle");
            return;
        }

        IsBusy = true;

        try
        {
            // Load current subscription status
            await LoadSubscriptionStatusAsync();

            // Load available plans
            await LoadAvailablePlansAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads the user's current subscription status including pending changes
    /// </summary>
    private async Task LoadSubscriptionStatusAsync()
    {
        if (_authService.CurrentUserId == null) return;

        var status = await _subscriptionService.GetSubscriptionStatusAsync(_authService.CurrentUserId.Value);

        if (status != null)
        {
            CurrentSubscription = status.CurrentSubscriptionType ?? Translator.T("Profile_NoSubscription");

            if (!string.IsNullOrEmpty(status.RenewalDate) && DateTime.TryParse(status.RenewalDate, out var renewalDate))
            {
                ExpiryDate = renewalDate;
                ExpiryDateDisplay = Translator.T("Subscription_ExpiresOn", renewalDate);
            }
            else
            {
                ExpiryDate = null;
                ExpiryDateDisplay = Translator.T("Subscription_NoExpiry");
            }

            // Check for pending subscription change
            if (!string.IsNullOrEmpty(status.PendingSubscriptionChange))
            {
                HasPendingChange = true;
                var billingCycle = Translator.T(status.PendingBillingCycle == "yearly" ? "Billing_Yearly" : "Billing_Monthly");
                PendingChangeMessage = Translator.T("Subscription_PendingMessage", status.PendingSubscriptionChange, billingCycle, ExpiryDate ?? default);
                System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Pending change detected: {PendingChangeMessage}");
            }
            else
            {
                HasPendingChange = false;
                PendingChangeMessage = string.Empty;
            }

            CheckExpiryNotification();
        }
    }

    private void CheckExpiryNotification()
    {
        if (ExpiryDate == null) return;

        var daysLeft = (ExpiryDate.Value.Date - DateTime.Today).Days;
        if (daysLeft < 0 || daysLeft > 7) return;

        // Maximaal één keer per dag notificeren
        var lastNotified = Preferences.Get("sub_expiry_notified", "");
        var todayStr     = DateTime.Today.ToString("yyyy-MM-dd");
        if (lastNotified == todayStr) return;

        Preferences.Set("sub_expiry_notified", todayStr);
        var userId = _authService.CurrentUserId;
        if (userId is null or <= 0) return;
        _notificationService.Add(
            userId.Value,
            Translator.T("Notif_ExpiringTitle"),
            Translator.T("Notif_ExpiringBody", CurrentSubscription, ExpiryDate.Value),
            NotificationType.SubscriptionExpiring);
    }

    /// <summary>
    /// Loads available subscription plans from the API
    /// </summary>
    private async Task LoadAvailablePlansAsync()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();

        AvailableSubscriptions.Clear();

        foreach (var plan in plans)
        {
            AvailableSubscriptions.Add(new SubscriptionOption
            {
                Name = plan.Name,
                MonthlyPrice = plan.MonthlyPrice,
                YearlyPrice = plan.YearlyPrice,
                Credits = plan.Credits,
                IsUnlimited = plan.IsUnlimited,
                Description = plan.Description,
                // Pass the ViewModel instance so the option can trigger subscription changes
                ParentViewModel = this
            });
        }

        System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Loaded {AvailableSubscriptions.Count} subscription plans");
    }

    /// <summary>
    /// Handles the yearly/monthly toggle change
    /// </summary>
    [RelayCommand]
    private void ToggleBillingCycle()
    {
        IsYearly = !IsYearly;
        System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Billing cycle toggled to: {(IsYearly ? "Yearly" : "Monthly")}");
    }

    /// <summary>
    /// Request a subscription change
    /// This will be applied on the next renewal date
    /// </summary>
    public async Task<bool> ChangeSubscriptionAsync(string newSubscriptionType)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUserId == null)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_Error"), Translator.T("Subscription_MustLoginChange"), Translator.T("Common_OK"));
            return false;
        }

        // Don't allow changing to the same subscription
        if (newSubscriptionType == CurrentSubscription && !HasPendingChange)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_Info"), Translator.T("Subscription_AlreadyHave"), Translator.T("Common_OK"));
            return false;
        }

        // Confirm subscription change
        var billingCycle = Translator.T(IsYearly ? "Billing_Yearly" : "Billing_Monthly");
        var price = IsYearly
            ? AvailableSubscriptions.FirstOrDefault(s => s.Name == newSubscriptionType)?.YearlyPrice ?? 0
            : AvailableSubscriptions.FirstOrDefault(s => s.Name == newSubscriptionType)?.MonthlyPrice ?? 0;

        var confirm = await Shell.Current.DisplayAlert(
            Translator.T("Subscription_ChangeTitle"),
            Translator.T("Subscription_ChangeConfirm", newSubscriptionType, billingCycle, price, ExpiryDate ?? default),
            Translator.T("Subscription_YesChange"),
            Translator.T("Common_Cancel")
        );

        if (!confirm) return false;

        IsBusy = true;

        try
        {
            var result = await _subscriptionService.RequestSubscriptionChangeAsync(
                _authService.CurrentUserId.Value,
                newSubscriptionType,
                IsYearly
            );

            if (result.Success)
            {
                var message = Translator.T("Subscription_ChangeSuccess", result.Message);
                await Shell.Current.DisplayAlert(Translator.T("Subscription_SuccessTitle"), message, Translator.T("Common_OK"));

                // Reload subscription status to show the pending change
                await LoadSubscriptionStatusAsync();
                return true;
            }
            else
            {
                await Shell.Current.DisplayAlert(Translator.T("Common_Error"), result.Message, Translator.T("Common_OK"));
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Error changing subscription: {ex.Message}");
            await Shell.Current.DisplayAlert(Translator.T("Common_Error"), Translator.T("Common_ErrorRetry"), Translator.T("Common_OK"));
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cancel the current subscription (will be effective on renewal date)
    /// </summary>
    [RelayCommand]
    public async Task CancelSubscription()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUserId == null)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_Error"), Translator.T("Subscription_MustLoginCancel"), Translator.T("Common_OK"));
            return;
        }

        if (string.IsNullOrEmpty(CurrentSubscription) || CurrentSubscription == Translator.T("Profile_NoSubscription"))
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_Info"), Translator.T("Subscription_NoActive"), Translator.T("Common_OK"));
            return;
        }

        // Confirm cancellation
        var confirm = await Shell.Current.DisplayAlert(
            Translator.T("Subscription_CancelTitle"),
            Translator.T("Subscription_CancelConfirm", CurrentSubscription, ExpiryDate ?? default),
            Translator.T("Subscription_YesCancel"),
            Translator.T("Common_Cancel")
        );

        if (!confirm) return;

        IsBusy = true;

        try
        {
            var result = await _subscriptionService.CancelSubscriptionAsync(_authService.CurrentUserId.Value);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert(Translator.T("Subscription_SuccessTitle"), result.Message, Translator.T("Common_OK"));

                // Reload subscription status
                await LoadSubscriptionStatusAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert(Translator.T("Common_Error"), result.Message, Translator.T("Common_OK"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Error cancelling subscription: {ex.Message}");
            await Shell.Current.DisplayAlert(Translator.T("Common_Error"), Translator.T("Common_ErrorRetry"), Translator.T("Common_OK"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Change the billing cycle for the current subscription
    /// </summary>
    [RelayCommand]
    public async Task ChangeBillingCycleForCurrentSubscription()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUserId == null)
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_Error"), Translator.T("Subscription_MustLoginBilling"), Translator.T("Common_OK"));
            return;
        }

        if (string.IsNullOrEmpty(CurrentSubscription) || CurrentSubscription == Translator.T("Profile_NoSubscription"))
        {
            await Shell.Current.DisplayAlert(Translator.T("Common_Info"), Translator.T("Subscription_NoActive"), Translator.T("Common_OK"));
            return;
        }

        var newBillingCycle = Translator.T(IsYearly ? "Billing_Yearly" : "Billing_Monthly");
        var oldBillingCycle = Translator.T(IsYearly ? "Billing_Monthly" : "Billing_Yearly");

        // Confirm billing cycle change
        var confirm = await Shell.Current.DisplayAlert(
            Translator.T("Subscription_BillingTitle"),
            Translator.T("Subscription_BillingConfirm", oldBillingCycle, newBillingCycle, CurrentSubscription, ExpiryDate ?? default),
            Translator.T("Subscription_YesChange"),
            Translator.T("Common_Cancel")
        );

        if (!confirm) return;

        IsBusy = true;

        try
        {
            var result = await _subscriptionService.ChangeBillingCycleAsync(
                _authService.CurrentUserId.Value,
                IsYearly
            );

            if (result.Success)
            {
                await Shell.Current.DisplayAlert(Translator.T("Subscription_SuccessTitle"), result.Message, Translator.T("Common_OK"));

                // Reload subscription status
                await LoadSubscriptionStatusAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert(Translator.T("Common_Error"), result.Message, Translator.T("Common_OK"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Error changing billing cycle: {ex.Message}");
            await Shell.Current.DisplayAlert(Translator.T("Common_Error"), Translator.T("Common_ErrorRetry"), Translator.T("Common_OK"));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Represents a subscription plan option with all its details
/// </summary>
public partial class SubscriptionOption : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int Credits { get; set; }
    public bool IsUnlimited { get; set; }
    public string Description { get; set; } = string.Empty;

    // Reference to parent ViewModel to trigger subscription changes
    public SubscriptionViewModel? ParentViewModel { get; set; }

    /// <summary>
    /// Display text for credits (handles unlimited case)
    /// </summary>
    public string CreditsDisplay => IsUnlimited ? Translator.T("Subscription_UnlimitedCredits") : Translator.T("Subscription_CreditsPerMonth", Credits);

    /// <summary>
    /// Display text for lessons (handles unlimited case)
    /// </summary>
    public string LessonsDisplay => IsUnlimited ? Translator.T("Subscription_UnlimitedLessons") : Translator.T("Subscription_LessonsPerMonth", Credits);

    /// <summary>
    /// Command to select this subscription
    /// </summary>
    [RelayCommand]
    private async Task SelectSubscription()
    {
        if (ParentViewModel != null)
        {
            await ParentViewModel.ChangeSubscriptionAsync(Name);
        }
    }
}
