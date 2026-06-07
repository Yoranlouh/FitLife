using Microsoft.Extensions.DependencyInjection;

namespace FitLife.Maui.Views;

/// <summary>
/// Splash screen displayed on app startup with FitLife branding
/// Automatically navigates to login page after a short delay
/// This page is shown BEFORE the Shell is initialized (pre-authentication)
/// </summary>
public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// When page appears, wait 2 seconds then navigate to login page
    /// Uses App.Current.MainPage to navigate outside of Shell
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Wait 2 seconds to show splash screen
        await Task.Delay(2000);

        // Navigate to LoginPage (still outside Shell)
        // We use MainPage replacement since we're not in Shell yet
        if (Application.Current != null)
        {
            // Get LoginViewModel and LoginPage from DI
            var services = Application.Current.Handler?.MauiContext?.Services;
            if (services != null)
            {
                var loginViewModel = services.GetService<ViewModels.LoginViewModel>();
                var loginPage = services.GetService<LoginPage>();

                if (loginPage != null && Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = loginPage;
                }
            }
        }
    }
}
