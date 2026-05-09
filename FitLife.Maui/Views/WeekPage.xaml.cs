namespace FitLife.Maui.Views;
using FitLife.Maui.ViewModels;

public partial class WeekPage : ContentPage
{
	public WeekPage(WeekViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
