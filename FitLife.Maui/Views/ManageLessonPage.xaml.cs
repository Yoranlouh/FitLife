using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class ManageLessonPage : ContentPage
{
    public ManageLessonPage(ManageLessonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ManageLessonViewModel vm)
            await vm.LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }
}