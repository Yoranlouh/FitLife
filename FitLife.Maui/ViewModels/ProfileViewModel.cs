using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitLife.Maui.ViewModels;

// ViewModel for the user profile page.
// Shows the logged-in user's name, photo, subscription plan, and credit balance.
// Also provides commands to change the profile photo and log out.
public partial class ProfileViewModel : BaseViewModel
{
    private readonly IAuthenticationService  _authService;
    private readonly ISubscriptionService?   _subscriptionService;
    private readonly IPhotoService           _photoService;
    private readonly INotificationService    _notificationService;

    // Name parts split from the full display name (e.g. "Jan" and "de Vries")
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    // URL of the user's profile photo — used by the UserAvatarView custom control
    [ObservableProperty]
    private string? _profilePictureUrl;

    // Current subscription tier (e.g. "Rookie", "Intermediate", "Advanced")
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditsDisplay))]
    private string _subscriptionName = "Geen abonnement";

    // When the subscription renews or expires
    [ObservableProperty]
    private DateTime? _subscriptionEndDate = null;

    // Remaining credits (null before data loads)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditsDisplay))]
    private int? _credits = null;

    // Shows "Onbeperkt" for Advanced subscribers, otherwise the raw credit count
    public string CreditsDisplay =>
        SubscriptionName == "Advanced" || Credits is null or >= 999
            ? "Onbeperkt"
            : Credits.Value.ToString();

    // True when the API has returned a pending subscription change — shows a warning card
    [ObservableProperty]
    private bool _hasPendingSubscriptionChange = false;

    // Human-readable description of the pending change, e.g. "Wijzigt naar Rookie (maandelijks) op 01-07-2024"
    [ObservableProperty]
    private string _pendingSubscriptionInfo = string.Empty;

    // True while a photo upload is in progress — shows a spinner over the avatar
    [ObservableProperty]
    private bool _isUploadingPhoto = false;

    public ProfileViewModel(IAuthenticationService authService,
                            IPhotoService          photoService,
                            INotificationService   notificationService,
                            ISubscriptionService?  subscriptionService = null)
    {
        _authService         = authService;
        _photoService        = photoService;
        _notificationService = notificationService;
        _subscriptionService = subscriptionService;
        Title = "Mijn Profiel";
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Constructor called. AuthService instance: {authService.GetHashCode()}");
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] AuthService IsAuthenticated: {authService.IsAuthenticated}");
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] SubscriptionService available: {_subscriptionService != null}");
        LoadUserData();
    }

    // Refreshes all user data and subscription info.
    // Called each time the profile page appears to keep the display current.
    public async Task RefreshUserDataAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] RefreshUserDataAsync called");
        LoadUserData();
        await LoadPendingSubscriptionChangeAsync();
    }

    // Synchronous version kept for backwards compatibility with code that can't await.
    public void RefreshUserData()
    {
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] RefreshUserData called");
        LoadUserData();
        _ = LoadPendingSubscriptionChangeAsync();
    }

    // Reads the current user's data from the in-memory IAuthenticationService
    // (no network call required — data was cached at login time).
    private void LoadUserData()
    {
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] LoadUserData called. IsAuthenticated: {_authService.IsAuthenticated}");

        if (_authService.IsAuthenticated)
        {
            var displayName = _authService.CurrentUserName ?? "Gebruiker";
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] DisplayName from auth service: {displayName}");

            // Split the full name on the first space to get first and last name parts
            var nameParts = displayName.Split(' ');
            FirstName = nameParts.Length > 0 ? nameParts[0] : displayName;
            LastName  = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Set FirstName: {FirstName}, LastName: {LastName}");

            // The photo URL is sourced from the auth service, which is updated after each
            // successful upload via IAuthenticationService.UpdatePhotoUrl()
            ProfilePictureUrl = _authService.CurrentUserPhotoUrl;
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] ProfilePictureUrl: {ProfilePictureUrl}");

            Credits          = _authService.CurrentUserCredits;
            SubscriptionName = _authService.CurrentUserSubscriptionType ?? "Geen abonnement";

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Credits: {Credits}, Subscription: {SubscriptionName}");

            if (!string.IsNullOrEmpty(_authService.CurrentUserSubscriptionRenewalDate)
                && DateTime.TryParse(_authService.CurrentUserSubscriptionRenewalDate, out var renewalDate))
                SubscriptionEndDate = renewalDate;
            else
                SubscriptionEndDate = null;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ProfileViewModel] User is not authenticated - skipping data load");
        }
    }

    // Fetches live subscription data from the API — needed for pending-change info
    // because that is not stored in the login response.
    private async Task LoadPendingSubscriptionChangeAsync()
    {
        if (_subscriptionService == null || !_authService.IsAuthenticated || _authService.CurrentUserId == null)
        {
            HasPendingSubscriptionChange = false;
            PendingSubscriptionInfo      = string.Empty;
            return;
        }

        try
        {
            var status = await _subscriptionService.GetSubscriptionStatusAsync(_authService.CurrentUserId.Value);
            if (status == null) return;

            // Refresh live values — these may differ from the login-time snapshot if the
            // subscription was changed in another session or from the web portal
            Credits = status.Credits;
            if (!string.IsNullOrEmpty(status.CurrentSubscriptionType))
                SubscriptionName = status.CurrentSubscriptionType;

            if (!string.IsNullOrEmpty(status.RenewalDate)
                && DateTime.TryParse(status.RenewalDate, out var renewalDate))
                SubscriptionEndDate = renewalDate;

            if (!string.IsNullOrEmpty(status.PendingSubscriptionChange))
            {
                HasPendingSubscriptionChange = true;
                var billingCycle    = status.PendingBillingCycle == "yearly" ? "jaarlijks" : "maandelijks";
                PendingSubscriptionInfo = $"Wijzigt naar {status.PendingSubscriptionChange} ({billingCycle}) op {SubscriptionEndDate:dd-MM-yyyy}";
                System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Pending change: {PendingSubscriptionInfo}");
            }
            else
            {
                HasPendingSubscriptionChange = false;
                PendingSubscriptionInfo      = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Error loading subscription status: {ex.Message}");
            HasPendingSubscriptionChange = false;
            PendingSubscriptionInfo      = string.Empty;
        }
    }

    // Opens the OS photo picker / camera dialog, uploads the chosen image to the API,
    // and updates both the profile view and the cached auth service URL.
    [RelayCommand]
    private async Task PickPhoto()
    {
        if (_authService.CurrentUserId is not int userId) return;

        IsUploadingPhoto = true;
        try
        {
            var newUrl = await _photoService.PickAndUploadAsync(userId);
            if (newUrl != null)
            {
                // Update the auth service cache so subsequent LoadUserData() calls
                // return the new URL instead of overwriting it with the old one
                _authService.UpdatePhotoUrl(newUrl);
                ProfilePictureUrl = newUrl;

                await Shell.Current.DisplayAlert("Gelukt", "Profielfoto bijgewerkt.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Photo upload error: {ex.Message}");
            await Shell.Current.DisplayAlert("Fout", "Foto uploaden mislukt. Controleer je verbinding.", "OK");
        }
        finally
        {
            IsUploadingPhoto = false;
        }
    }

    // Clears the authentication session and replaces the app's root page with the login page.
    [RelayCommand]
    private async Task Logout()
    {
        System.Diagnostics.Debug.WriteLine("[ProfileViewModel] Logout called");
        await _authService.LogoutAsync();
        _notificationService.Clear();

        // Navigate outside the Shell by replacing the window root page directly
        if (Application.Current?.Windows.Count > 0)
        {
            var services  = Application.Current.Handler?.MauiContext?.Services;
            if (services != null)
            {
                var loginPage = services.GetService<Views.LoginPage>();
                if (loginPage != null)
                {
                    Application.Current.Windows[0].Page = loginPage;
                    return;
                }
            }

            // Fallback when DI resolution fails
            System.Diagnostics.Debug.WriteLine("[ProfileViewModel] LoginPage not found in DI, attempting fallback");
            Application.Current.Windows[0].Page = new Views.SplashPage();
        }
    }
}
