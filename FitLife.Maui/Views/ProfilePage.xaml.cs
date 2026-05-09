using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

public partial class ProfilePage : ContentPage
{
	public ProfilePage(ProfileViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private async void OnManageSubscriptionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SubscriptionPage");
    }

    private async void OnViewMyLessonsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("MyLessonsPage");
    }
}
