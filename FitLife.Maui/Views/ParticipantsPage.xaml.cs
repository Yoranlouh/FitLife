namespace FitLife.Maui.Views;

public partial class ParticipantsPage : ContentPage
{
	public ParticipantsPage(ViewModels.ParticipantsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }
}