using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitLife.Maui.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly ISubscriptionService?  _subscriptionService;
    private readonly IPhotoService          _photoService;

    // ObservableProperty fields - these will be populated from authentication service when user logs in
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string? _profilePictureUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditsDisplay))]
    private string _subscriptionName = "Geen abonnement";

    [ObservableProperty]
    private DateTime? _subscriptionEndDate = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreditsDisplay))]
    private int? _credits = null;

    public string CreditsDisplay =>
        SubscriptionName == "Advanced" || Credits is null or >= 999
            ? "Onbeperkt"
            : Credits.Value.ToString();

    [ObservableProperty]
    private bool _hasPendingSubscriptionChange = false;

    [ObservableProperty]
    private string _pendingSubscriptionInfo = string.Empty;

    [ObservableProperty]
    private bool _isUploadingPhoto = false;

    public ProfileViewModel(IAuthenticationService authService, IPhotoService photoService, ISubscriptionService? subscriptionService = null)
    {
        _authService         = authService;
        _photoService        = photoService;
        _subscriptionService = subscriptionService;
        Title = "Mijn Profiel";
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Constructor called. AuthService instance: {authService.GetHashCode()}");
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] AuthService IsAuthenticated: {authService.IsAuthenticated}");
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] SubscriptionService available: {_subscriptionService != null}");
        LoadUserData();
    }

    /// <summary>
    /// Refreshes user data from the authentication service
    /// Call this method when the profile page appears to ensure data is up-to-date
    /// </summary>
    public async Task RefreshUserDataAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] RefreshUserDataAsync called");
        LoadUserData();
        await LoadPendingSubscriptionChangeAsync();
    }

    /// <summary>
    /// Legacy synchronous version for backward compatibility
    /// </summary>
    public void RefreshUserData()
    {
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] RefreshUserData called");
        LoadUserData();
        _ = LoadPendingSubscriptionChangeAsync();
    }

    /// <summary>
    /// Loads user data from the authentication service and updates all properties
    /// </summary>
    private void LoadUserData()
    {
        // Debug logging to help diagnose issues
        System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] LoadUserData called. IsAuthenticated: {_authService.IsAuthenticated}");

        if (_authService.IsAuthenticated)
        {
            // Get user name from authentication service
            var displayName = _authService.CurrentUserName ?? "Gebruiker";
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] DisplayName from auth service: {displayName}");

            var nameParts = displayName.Split(' ');

            FirstName = nameParts.Length > 0 ? nameParts[0] : displayName;
            LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Set FirstName: {FirstName}, LastName: {LastName}");

            ProfilePictureUrl = _authService.CurrentUserPhotoUrl;

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] ProfilePictureUrl: {ProfilePictureUrl}");

            // Get subscription and credits information
            Credits = _authService.CurrentUserCredits;
            SubscriptionName = _authService.CurrentUserSubscriptionType ?? "Geen abonnement";

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Credits: {Credits}, Subscription: {SubscriptionName}");

            if (!string.IsNullOrEmpty(_authService.CurrentUserSubscriptionRenewalDate)
                && DateTime.TryParse(_authService.CurrentUserSubscriptionRenewalDate, out var renewalDate))
            {
                SubscriptionEndDate = renewalDate;
            }
            else
            {
                SubscriptionEndDate = null;
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ProfileViewModel] User is not authenticated - skipping data load");
        }
    }

    /// <summary>
    /// Fetches live data from the API: refreshes credits, subscription type, renewal date,
    /// and any pending subscription change.
    /// </summary>
    private async Task LoadPendingSubscriptionChangeAsync()
    {
        if (_subscriptionService == null || !_authService.IsAuthenticated || _authService.CurrentUserId == null)
        {
            HasPendingSubscriptionChange = false;
            PendingSubscriptionInfo = string.Empty;
            return;
        }

        try
        {
            var status = await _subscriptionService.GetSubscriptionStatusAsync(_authService.CurrentUserId.Value);

            if (status == null) return;

            // Refresh live values from the API so they reflect the current DB state.
            Credits = status.Credits;
            if (!string.IsNullOrEmpty(status.CurrentSubscriptionType))
                SubscriptionName = status.CurrentSubscriptionType;

            if (!string.IsNullOrEmpty(status.RenewalDate)
                && DateTime.TryParse(status.RenewalDate, out var renewalDate))
            {
                SubscriptionEndDate = renewalDate;
            }

            if (!string.IsNullOrEmpty(status.PendingSubscriptionChange))
            {
                HasPendingSubscriptionChange = true;
                var billingCycle = status.PendingBillingCycle == "yearly" ? "jaarlijks" : "maandelijks";
                PendingSubscriptionInfo = $"Wijzigt naar {status.PendingSubscriptionChange} ({billingCycle}) op {SubscriptionEndDate:dd-MM-yyyy}";
                System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Pending change: {PendingSubscriptionInfo}");
            }
            else
            {
                HasPendingSubscriptionChange = false;
                PendingSubscriptionInfo = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Error loading subscription status: {ex.Message}");
            HasPendingSubscriptionChange = false;
            PendingSubscriptionInfo = string.Empty;
        }
    }

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
                ProfilePictureUrl = newUrl;
                await Application.Current!.MainPage!.DisplayAlert(
                    "Gelukt", "Profielfoto bijgewerkt.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Photo upload error: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert(
                "Fout", "Foto uploaden mislukt. Controleer je verbinding.", "OK");
        }
        finally
        {
            IsUploadingPhoto = false;
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        System.Diagnostics.Debug.WriteLine("[ProfileViewModel] Logout called");
        await _authService.LogoutAsync();

        // Instead of Shell navigation, replace the MainPage with LoginPage
        // because LoginPage is outside of the AppShell in this project structure
        if (Application.Current != null)
        {
            var services = Application.Current.Handler?.MauiContext?.Services;
            if (services != null)
            {
                var loginPage = services.GetService<Views.LoginPage>();
                if (loginPage != null)
                {
                    Application.Current.MainPage = loginPage;
                    return;
                }
            }
            
            // Fallback if DI service is not available (though it should be)
            // Note: Creating a new LoginPage might lose DI benefits for its ViewModel
            // but is better than the app closing or doing nothing
            System.Diagnostics.Debug.WriteLine("[ProfileViewModel] LoginPage not found in DI, attempting fallback");
            // If DI fails, we might need to manually resolve or just navigate back to Splash
            Application.Current.MainPage = new Views.SplashPage();
        }
    }
}
