using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class InstructorLessonsPage : ContentPage
{
    public InstructorLessonsPage(InstructorLessonsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is InstructorLessonsViewModel vm)
            await vm.LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}