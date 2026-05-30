using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HomeViewModel vm)
            vm.RefreshRole();
    }

    private async void OnViewLessonsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LessonsPage");
    }

    private async void OnReservationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MyLessonsPage");
    }

    private async void OnInstructorLessonsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//InstructorLessonsPage");
    }

    private async void OnCreateLessonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ManageLessonPage");
    }
}