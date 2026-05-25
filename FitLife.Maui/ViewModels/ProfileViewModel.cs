using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using SharedLibrary.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace FitLife.Maui.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly ISubscriptionService? _subscriptionService;

    // ObservableProperty fields - these will be populated from authentication service when user logs in
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _profilePictureUrl = "https://ui-avatars.com/api/?name=User&size=128";

    [ObservableProperty]
    private string _subscriptionName = "Geen abonnement";

    [ObservableProperty]
    private DateTime? _subscriptionEndDate = null;

    [ObservableProperty]
    private int _credits = 0;

    [ObservableProperty]
    private bool _hasPendingSubscriptionChange = false;

    [ObservableProperty]
    private string _pendingSubscriptionInfo = string.Empty;

    public ProfileViewModel(IAuthenticationService authService, ISubscriptionService? subscriptionService = null)
    {
        _authService = authService;
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

            // Get profile photo URL from database, or fallback to generated avatar
            ProfilePictureUrl = _authService.CurrentUserPhotoUrl
                ?? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(displayName)}&size=200&background=6366F1&color=fff";

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] ProfilePictureUrl: {ProfilePictureUrl}");

            // Get subscription and credits information
            Credits = _authService.CurrentUserCredits ?? 0;
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
    /// Loads any pending subscription change information from the API
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

            if (status != null && !string.IsNullOrEmpty(status.PendingSubscriptionChange))
            {
                HasPendingSubscriptionChange = true;
                var billingCycle = status.PendingBillingCycle == "yearly" ? "jaarlijks" : "maandelijks";

                if (!string.IsNullOrEmpty(status.RenewalDate) && DateTime.TryParse(status.RenewalDate, out var renewalDate))
                {
                    PendingSubscriptionInfo = $"Wijzigt naar {status.PendingSubscriptionChange} ({billingCycle}) op {renewalDate:dd-MM-yyyy}";
                }
                else
                {
                    PendingSubscriptionInfo = $"Wijzigt naar {status.PendingSubscriptionChange} ({billingCycle})";
                }

                System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Pending subscription change detected: {PendingSubscriptionInfo}");
            }
            else
            {
                HasPendingSubscriptionChange = false;
                PendingSubscriptionInfo = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Error loading pending subscription change: {ex.Message}");
            HasPendingSubscriptionChange = false;
            PendingSubscriptionInfo = string.Empty;
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
