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

    private async void OnBackToHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}