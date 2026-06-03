using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
		BindingContext = _viewModel;
	}

    /// <summary>
    /// Called when the page appears - refreshes user data to ensure it's current
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Refresh user data every time the profile page is displayed
        // This ensures the correct user name and data are shown after login
        // Also loads any pending subscription changes
        await _viewModel.RefreshUserDataAsync();
    }

    private async void OnManageSubscriptionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SubscriptionPage");
    }

    private async void OnViewMyLessonsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("MyLessonsPage");
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}