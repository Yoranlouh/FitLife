using CommunityToolkit.Mvvm.ComponentModel;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

// ViewModel for the home/dashboard page.
// Displays role-specific content (admin, instructor, or member view)
// and the unread notification badge count.
public partial class HomeViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly INotificationService   _notificationService;

    // Role flags — determine which tiles and menu items are shown on the home page
    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _isInstructor;

    [ObservableProperty]
    private bool _isMember;

    // Personalised welcome text shown at the top of the home screen
    [ObservableProperty]
    private string _welcomeMessage = "Welkom bij FitLife";

    // Number of unread notifications — drives the badge on the bell icon.
    // NotifyPropertyChangedFor ensures HasUnread also re-evaluates when this changes.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnread))]
    private int _unreadCount;

    // True when there is at least one unread notification; controls badge visibility
    public bool HasUnread => UnreadCount > 0;

    public HomeViewModel(IAuthenticationService authService, INotificationService notificationService)
    {
        _authService         = authService;
        _notificationService = notificationService;
        Title = "Hoofdmenu";
        RefreshRole();
    }

    // Reads the current user's role from the authentication service and updates
    // the role flags and welcome message. Called after login and on page appearance.
    public void RefreshRole()
    {
        IsAdmin      = _authService.IsAdmin;
        IsInstructor = _authService.IsInstructor;
        IsMember     = _authService.IsMember;

        WelcomeMessage = IsAdmin      ? "Welkom, Admin"
            : IsInstructor ? "Welkom, Trainer"
            : "Welkom bij FitLife";
    }

    // Reads the current unread notification count from the in-memory service.
    // Called by the view on appearance so the badge stays current.
    public void RefreshUnreadCount()
    {
        UnreadCount = _notificationService.UnreadCount;
    }
}
