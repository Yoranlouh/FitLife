using FitLife.Maui.Controls;
using FitLife.Maui.Services;
using FitLife.Maui.Views;

namespace FitLife.Maui;

// The application shell — the root navigation container for the MAUI app.
// Shell provides a flyout (hamburger menu) and handles page routing.
// It is rebuilt after each login so the menu reflects the user's role
// (member, instructor, or admin).
public partial class AppShell : Shell
{
    // Flyout items keyed by translation key so the menu can be re-translated
    // in place when the language changes (FlyoutItem.Title is not bindable here
    // because the items are built in code).
    private readonly List<(BaseShellItem Item, string TitleKey)> _translatableItems = new();
    private MenuItem? _logoutItem;
    private Label?    _roleLabel;
    private string    _roleKey = "Role_Member";

    private System.ComponentModel.PropertyChangedEventHandler? _translatorHandler;

    public AppShell(IAuthenticationService authService, IServiceProvider services)
    {
        InitializeComponent();

        // Re-translate the flyout menu whenever the language changes.
        // AppShell is transient (rebuilt per login), so the handler unsubscribes
        // itself once this shell is no longer the active one.
        _translatorHandler = (_, _) =>
        {
            if (Current != this)
            {
                Translator.Instance.PropertyChanged -= _translatorHandler;
                return;
            }
            ApplyMenuTranslations();
        };
        Translator.Instance.PropertyChanged += _translatorHandler;

        // Register push-routes: pages that are navigated to with GoToAsync("RouteName")
        // but are not items in the flyout menu.
        Routing.RegisterRoute("DayPage",           typeof(DayPage));
        Routing.RegisterRoute("LessonDetailPage",  typeof(LessonDetailPage));
        Routing.RegisterRoute("ParticipantsPage",  typeof(ParticipantsPage));
        Routing.RegisterRoute("SubscriptionPage",  typeof(SubscriptionPage));
        Routing.RegisterRoute("ManageLessonPage",  typeof(ManageLessonPage));
        Routing.RegisterRoute("SettingsPage",      typeof(SettingsPage));
        Routing.RegisterRoute("MySportclubPage",   typeof(MySportclubPage));
        Routing.RegisterRoute("NotificationsPage", typeof(NotificationsPage));

        BuildFlyout(authService, services);
    }

    // Constructs the flyout menu items in order.
    // Items marked as hidden are still reachable via GoToAsync("//RouteName")
    // but do not appear in the side menu.
    private void BuildFlyout(IAuthenticationService authService, IServiceProvider services)
    {
        // The top of the flyout shows the user's avatar, name, and role
        FlyoutHeader         = BuildFlyoutHeader(authService);
        FlyoutHeaderBehavior = FlyoutHeaderBehavior.Fixed;

        // ── Visible flyout items ──────────────────────────────────────────

        AddTranslatedFlyoutItem("Menu_Home",
            "icon_home.svg",
            services.GetRequiredService<HomePage>(),
            "HomePage");

        AddTranslatedFlyoutItem("Menu_Settings",
            "icon_settings.svg",
            services.GetRequiredService<SettingsPage>(),
            "SettingsPage");

        AddTranslatedFlyoutItem("Menu_MySportclub",
            "icon_sportclub.svg",
            services.GetRequiredService<MySportclubPage>(),
            "MySportclubPage");

        // Logout is a MenuItem (triggers an action) rather than a FlyoutItem (navigates to a page)
        _logoutItem = new MenuItem
        {
            Text            = Translator.T("Menu_Logout"),
            IconImageSource = ImageSource.FromFile("icon_logout.svg"),
            Command         = new Command(async () => await PerformLogoutAsync(authService))
        };
        Items.Add(_logoutItem);

        AddTranslatedFlyoutItem("Menu_MyAccount",
            "icon_profile_white.svg",
            services.GetRequiredService<ProfilePage>(),
            "ProfilePage");

        AddTranslatedFlyoutItem("Menu_Reservations",
            "icon_bookmark_white.svg",
            services.GetRequiredService<MyLessonsPage>(),
            "MyLessonsPage");

        AddTranslatedFlyoutItem("Menu_Schedule",
            "icon_calendar_white.svg",
            services.GetRequiredService<WeekPage>(),
            "WeekPage");

        // ── Hidden items: navigable via //Route but not shown in the flyout ──

        // LessonsPage is opened from home screen tiles
        Items.Add(MakeHiddenFlyoutItem(
            services.GetRequiredService<LessonsPage>(), "LessonsPage"));

        // InstructorLessonsPage is opened from the instructor home screen tile
        Items.Add(MakeHiddenFlyoutItem(
            services.GetRequiredService<InstructorLessonsPage>(), "InstructorLessonsPage"));
    }

    // Creates a visible FlyoutItem with an icon, translated title, and a single
    // content page, and registers it for live re-translation on language change.
    private void AddTranslatedFlyoutItem(
        string titleKey, string iconFile, ContentPage page, string route)
    {
        var item = new FlyoutItem { Title = Translator.T(titleKey) };
        item.FlyoutIcon = ImageSource.FromFile(iconFile);
        var content = new ShellContent { Route = route, Content = page };
        item.Items.Add(content);
        _translatableItems.Add((item, titleKey));
        Items.Add(item);
    }

    // Re-applies translated titles to all flyout items, the logout entry,
    // and the role label in the flyout header.
    private void ApplyMenuTranslations()
    {
        foreach (var (item, key) in _translatableItems)
            item.Title = Translator.T(key);

        if (_logoutItem != null)
            _logoutItem.Text = Translator.T("Menu_Logout");

        if (_roleLabel != null)
            _roleLabel.Text = Translator.T(_roleKey);
    }

    // Creates a FlyoutItem that is hidden from the menu but still navigable by route.
    private static FlyoutItem MakeHiddenFlyoutItem(ContentPage page, string route)
    {
        var item = new FlyoutItem { Title = route };
        Shell.SetFlyoutItemIsVisible(item, false);
        var content = new ShellContent { Route = route, Content = page };
        item.Items.Add(content);
        return item;
    }

    // Builds the flyout header view — shown at the top of the hamburger menu.
    // Displays the user's avatar image (or default icon), name, and role label.
    private View BuildFlyoutHeader(IAuthenticationService authService)
    {
        var root = new Grid
        {
            BackgroundColor = Color.FromArgb("#111111"),
            Padding         = new Thickness(20, 52, 20, 24)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Avatar (uses the UserAvatarView custom control that handles default/photo states)
        var avatar = new UserAvatarView
        {
            PhotoUrl          = authService.CurrentUserPhotoUrl,
            AvatarSize        = 72,
            HorizontalOptions = LayoutOptions.Start
        };
        Grid.SetRow(avatar, 0);
        root.Children.Add(avatar);

        // Full display name in white text
        var nameLabel = new Label
        {
            Text           = authService.CurrentUserName ?? "Gebruiker",
            TextColor      = Colors.White,
            FontSize       = 17,
            FontAttributes = FontAttributes.Bold,
            Margin         = new Thickness(0, 14, 0, 3)
        };
        Grid.SetRow(nameLabel, 1);
        root.Children.Add(nameLabel);

        // Role badge in grey — maps internal role string to a translated label
        _roleKey = authService.CurrentUserRole?.ToLower() switch
        {
            "admin" or "employee" => "Role_Admin",
            "instructor"          => "Role_Instructor",
            _                     => "Role_Member"
        };
        _roleLabel = new Label
        {
            Text      = Translator.T(_roleKey),
            TextColor = Color.FromArgb("#9CA3AF"),
            FontSize  = 13
        };
        Grid.SetRow(_roleLabel, 2);
        root.Children.Add(_roleLabel);

        return root;
    }

    // Handles the logout flow:
    // 1. Closes the flyout
    // 2. Asks the user to confirm
    // 3. Clears the session
    // 4. Replaces the main page with the login page
    private static async Task PerformLogoutAsync(IAuthenticationService authService)
    {
        Current.FlyoutIsPresented = false;

        var confirmed = await Current.DisplayAlert(
            Translator.T("Logout_Title"),
            Translator.T("Logout_Confirm"),
            Translator.T("Logout_Yes"),
            Translator.T("Common_Cancel"));

        if (!confirmed) return;

        await authService.LogoutAsync();

        // Wipe in-memory notifications so the next account never sees this user's data.
        var appServices = Application.Current?.Handler?.MauiContext?.Services;
        appServices?.GetService<INotificationService>()?.Clear();

        // Navigate to the login page by replacing the root page.
        // Shell.GoToAsync cannot navigate outside the Shell hierarchy, so we
        // update the window's page directly (Windows[0].Page replaces the
        // deprecated Application.MainPage property).
        if (appServices != null)
        {
            var loginPage = appServices.GetService<Views.LoginPage>();
            if (loginPage != null && Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = loginPage;
                return;
            }
        }

        // Fallback if DI resolution fails
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = new Views.SplashPage();
    }
}
