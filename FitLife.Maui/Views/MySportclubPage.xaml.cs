namespace FitLife.Maui.Views;

public partial class MySportclubPage : ContentPage
{
    public MySportclubPage()
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
