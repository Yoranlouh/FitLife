namespace FitLife.Maui.Views;
using FitLife.Maui.ViewModels;

public partial class DayPage : ContentPage
{
	public DayPage(DayViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
