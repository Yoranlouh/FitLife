namespace FitLife.Maui.Views;
using FitLife.Maui.ViewModels;

public partial class LessonDetailPage : ContentPage
{
	public LessonDetailPage(LessonDetailViewModel viewModel)
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