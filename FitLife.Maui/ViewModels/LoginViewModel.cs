using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

// ViewModel for the login page.
// Validates credentials locally, calls the API, and navigates to the main Shell on success.
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INotificationService   _notificationService;

    // Two-way bound to the email and password text fields in the UI
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    // Displayed below the login button when validation or the API returns an error
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // True while the API call is in progress — shows a spinner and disables the button
    [ObservableProperty]
    private bool _isLoggingIn;

    public LoginViewModel(IAuthenticationService authenticationService,
                          INotificationService   notificationService)
    {
        _authenticationService = authenticationService;
        _notificationService   = notificationService;
        Title = "Inloggen";
    }

    // Validates the form, calls POST /auth/login, loads persisted notifications from the
    // database, then replaces the app's MainPage with the role-correct AppShell.
    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        // Client-side validation before making the API call
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Voer je email adres in.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Voer je wachtwoord in.";
            return;
        }

        IsLoggingIn = true;

        try
        {
            var result = await _authenticationService.LoginAsync(Email, Password);

            if (result.Success)
            {
                // Restore notifications saved from previous sessions
                if (result.UserId.HasValue)
                    await _notificationService.LoadAsync(result.UserId.Value);

                // Replace the root page with AppShell — the Shell builds role-based tabs/flyout
                if (Application.Current != null)
                {
                    var shell = IPlatformApplication.Current?.Services.GetRequiredService<AppShell>();
                    Application.Current.MainPage = shell ?? new AppShell(
                        IPlatformApplication.Current!.Services.GetRequiredService<IAuthenticationService>(),
                        IPlatformApplication.Current.Services);
                }
            }
            else
            {
                ErrorMessage = result.Message;  // show the server-provided error text
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Er is een fout opgetreden. Probeer het opnieuw.";
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    // Clears the error message as soon as the user starts editing the email field
    partial void OnEmailChanged(string value)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;
    }

    // Clears the error message as soon as the user starts editing the password field
    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;
    }
}
