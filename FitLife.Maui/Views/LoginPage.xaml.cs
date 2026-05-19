using FitLife.Maui.ViewModels;

namespace FitLife.Maui.Views;

/// <summary>
/// Login page where users enter their credentials to access the app
/// </summary>
public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
