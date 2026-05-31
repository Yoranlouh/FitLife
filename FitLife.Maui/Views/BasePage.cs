namespace FitLife.Maui.Views;

/// <summary>
/// Base page for all non-home pages.
/// Overrides the Android hardware back button to always navigate to the home screen.
/// </summary>
public class BasePage : ContentPage
{
    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true; // prevent default back navigation
    }

    protected async void OnBackToHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");
}