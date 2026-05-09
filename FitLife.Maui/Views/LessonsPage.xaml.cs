using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class LessonsPage : ContentPage
{
	public LessonsPage(LessonsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
