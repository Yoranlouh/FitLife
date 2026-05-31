using FitLife.Maui.Services;
using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class HomePage : ContentPage
{
    private readonly IAuthenticationService _authService;

    public HomePage(HomeViewModel viewModel, IAuthenticationService authService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _authService = authService;
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

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ProfilePage");
    }

    private async void OnSubscriptionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SubscriptionPage");
    }

    private void OnHamburgerClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}