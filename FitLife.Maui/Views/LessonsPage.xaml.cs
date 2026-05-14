using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class LessonsPage : ContentPage
{
	public LessonsPage(LessonsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LessonsViewModel vm)
        {
            await vm.LoadLessonsCommand.ExecuteAsync(null);
        }
    }
}
