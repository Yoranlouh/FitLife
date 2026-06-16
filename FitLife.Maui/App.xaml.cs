using FitLife.Maui.Views;

namespace FitLife.Maui;

/// <summary>
/// Main application class that initializes the app and sets up the Shell
/// Starts with SplashPage, then LoginPage, then navigates to main Shell after authentication
/// </summary>
public partial class App : Application
{
	public App()
	{
		// Restore the saved language (and matching culture) before any page is built.
		Services.Translator.Initialize();
		InitializeComponent();
	}

	/// <summary>
	/// Creates the main window and shows the SplashPage on startup
	/// SplashPage is shown first, outside of the Shell navigation
	/// </summary>
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new SplashPage());
	}
}
