using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

/// <summary>
/// ViewModel for the login page
/// Handles user input validation and authentication logic
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoggingIn;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        Title = "Inloggen";
    }

    /// <summary>
    /// Command to handle login button click
    /// Validates input, calls authentication service, and navigates on success
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        // Clear previous error message
        ErrorMessage = string.Empty;

        // Validate input
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

        // Show loading state
        IsLoggingIn = true;

        try
        {
            // Attempt login
            var result = await _authenticationService.LoginAsync(Email, Password);

            if (result.Success)
            {
                // Login successful - navigate to main app Shell via DI so role-based tabs are built correctly
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
                // Login failed - show error message
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            // Handle unexpected errors
            ErrorMessage = "Er is een fout opgetreden. Probeer het opnieuw.";
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
        }
        finally
        {
            // Hide loading state
            IsLoggingIn = false;
        }
    }

    /// <summary>
    /// Clear error message when user starts typing
    /// </summary>
    partial void OnEmailChanged(string value)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Clear error message when user starts typing
    /// </summary>
    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ErrorMessage = string.Empty;
        }
    }
}
