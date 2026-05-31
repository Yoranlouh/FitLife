namespace FitLife.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnBackToHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}
