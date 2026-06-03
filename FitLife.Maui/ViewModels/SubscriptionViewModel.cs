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
        Title = "Abonnement Beheren";
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
            CurrentSubscription = "Niet ingelogd";
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
            CurrentSubscription = status.CurrentSubscriptionType ?? "Geen abonnement";

            if (!string.IsNullOrEmpty(status.RenewalDate) && DateTime.TryParse(status.RenewalDate, out var renewalDate))
            {
                ExpiryDate = renewalDate;
                ExpiryDateDisplay = $"Verloopt op: {renewalDate:dd-MM-yyyy}";
            }
            else
            {
                ExpiryDate = null;
                ExpiryDateDisplay = "Geen vervaldatum";
            }

            // Check for pending subscription change
            if (!string.IsNullOrEmpty(status.PendingSubscriptionChange))
            {
                HasPendingChange = true;
                var billingCycle = status.PendingBillingCycle == "yearly" ? "jaarlijks" : "maandelijks";
                PendingChangeMessage = $"Je abonnement wordt gewijzigd naar {status.PendingSubscriptionChange} ({billingCycle}) op {ExpiryDate:dd-MM-yyyy}";
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
        _notificationService.Add(
            "Abonnement verloopt bijna",
            $"Je {CurrentSubscription} abonnement verloopt op {ExpiryDate.Value:d MMMM yyyy}. Verleng op tijd om te blijven sporten.",
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
            await Application.Current.MainPage.DisplayAlert("Fout", "Je moet ingelogd zijn om je abonnement te wijzigen.", "OK");
            return false;
        }

        // Don't allow changing to the same subscription
        if (newSubscriptionType == CurrentSubscription && !HasPendingChange)
        {
            await Application.Current.MainPage.DisplayAlert("Info", "Je hebt al dit abonnement.", "OK");
            return false;
        }

        // Confirm subscription change
        var billingCycle = IsYearly ? "jaarlijks" : "maandelijks";
        var price = IsYearly
            ? AvailableSubscriptions.FirstOrDefault(s => s.Name == newSubscriptionType)?.YearlyPrice ?? 0
            : AvailableSubscriptions.FirstOrDefault(s => s.Name == newSubscriptionType)?.MonthlyPrice ?? 0;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Abonnement wijzigen",
            $"Weet je zeker dat je wilt overstappen naar {newSubscriptionType} ({billingCycle}, €{price:F0})?\n\nDe wijziging wordt toegepast op {ExpiryDate:dd-MM-yyyy}.",
            "Ja, wijzigen",
            "Annuleren"
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
                var message = $"{result.Message}\n\nHet eventuele resterende saldo van je huidige abonnement wordt automatisch verrekend bij de eerstvolgende automatische incasso.";
                await Application.Current.MainPage.DisplayAlert("Gelukt!", message, "OK");

                // Reload subscription status to show the pending change
                await LoadSubscriptionStatusAsync();
                return true;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Fout", result.Message, "OK");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Error changing subscription: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fout", "Er is een fout opgetreden. Probeer het later opnieuw.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Fout", "Je moet ingelogd zijn om je abonnement te annuleren.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(CurrentSubscription) || CurrentSubscription == "Geen abonnement")
        {
            await Application.Current.MainPage.DisplayAlert("Info", "Je hebt geen actief abonnement.", "OK");
            return;
        }

        // Confirm cancellation
        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Abonnement stopzetten",
            $"Weet je zeker dat je je {CurrentSubscription} abonnement wilt stopzetten?\n\nJe abonnement blijft actief tot {ExpiryDate:dd-MM-yyyy}. Na deze datum wordt het niet verlengd.",
            "Ja, stopzetten",
            "Annuleren"
        );

        if (!confirm) return;

        IsBusy = true;

        try
        {
            var result = await _subscriptionService.CancelSubscriptionAsync(_authService.CurrentUserId.Value);

            if (result.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Gelukt!", result.Message, "OK");

                // Reload subscription status
                await LoadSubscriptionStatusAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Fout", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Error cancelling subscription: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fout", "Er is een fout opgetreden. Probeer het later opnieuw.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Fout", "Je moet ingelogd zijn om je factureringsperiode te wijzigen.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(CurrentSubscription) || CurrentSubscription == "Geen abonnement")
        {
            await Application.Current.MainPage.DisplayAlert("Info", "Je hebt geen actief abonnement.", "OK");
            return;
        }

        var newBillingCycle = IsYearly ? "jaarlijks" : "maandelijks";
        var oldBillingCycle = IsYearly ? "maandelijks" : "jaarlijks";

        // Confirm billing cycle change
        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Factureringsperiode wijzigen",
            $"Weet je zeker dat je wilt overstappen van {oldBillingCycle} naar {newBillingCycle} betalen?\n\nJe blijft je huidige {CurrentSubscription} abonnement behouden.\n\nDe wijziging wordt toegepast op {ExpiryDate:dd-MM-yyyy}.",
            "Ja, wijzigen",
            "Annuleren"
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
                await Application.Current.MainPage.DisplayAlert("Gelukt!", result.Message, "OK");

                // Reload subscription status
                await LoadSubscriptionStatusAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Fout", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SubscriptionViewModel] Error changing billing cycle: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fout", "Er is een fout opgetreden. Probeer het later opnieuw.", "OK");
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
    public string CreditsDisplay => IsUnlimited ? "Onbeperkt credits" : $"{Credits} credits per maand";

    /// <summary>
    /// Display text for lessons (handles unlimited case)
    /// </summary>
    public string LessonsDisplay => IsUnlimited ? "Onbeperkt lessen per maand" : $"{Credits} lessen per maand";

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
