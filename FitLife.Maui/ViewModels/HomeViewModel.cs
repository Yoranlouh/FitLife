using CommunityToolkit.Mvvm.ComponentModel;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _isInstructor;

    [ObservableProperty]
    private bool _isMember;

    [ObservableProperty]
    private string _welcomeMessage = "Welkom bij FitLife";

    public HomeViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Title = "Hoofdmenu";
        RefreshRole();
    }

    public void RefreshRole()
    {
        IsAdmin = _authService.IsAdmin;
        IsInstructor = _authService.IsInstructor;
        IsMember = _authService.IsMember;

        WelcomeMessage = IsAdmin ? "Welkom, Admin"
            : IsInstructor ? "Welkom, Trainer"
            : "Welkom bij FitLife";
    }
}