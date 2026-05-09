using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class HomePage : ContentPage
{
	public HomePage(HomeViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private async void OnViewLessonsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LessonsPage");
    }
}
