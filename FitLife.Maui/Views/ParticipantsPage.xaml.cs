namespace FitLife.Maui.Views;

public partial class ParticipantsPage : ContentPage
{
	public ParticipantsPage(ViewModels.ParticipantsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private async void OnBackToHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}