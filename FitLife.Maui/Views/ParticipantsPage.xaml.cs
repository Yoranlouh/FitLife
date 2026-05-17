namespace FitLife.Maui.Views;

public partial class ParticipantsPage : ContentPage
{
	public ParticipantsPage(ViewModels.ParticipantsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
