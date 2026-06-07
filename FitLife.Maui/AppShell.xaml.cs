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
    public AppShell(IAuthenticationService authService, IServiceProvider services)
    {
        InitializeComponent();

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

        Items.Add(MakeFlyoutItem("Home",
            "icon_home.svg",
            services.GetRequiredService<HomePage>(),
            "HomePage"));

        Items.Add(MakeFlyoutItem("Instellingen",
            "icon_settings.svg",
            services.GetRequiredService<SettingsPage>(),
            "SettingsPage"));

        Items.Add(MakeFlyoutItem("Mijn sportclub",
            "icon_sportclub.svg",
            services.GetRequiredService<MySportclubPage>(),
            "MySportclubPage"));

        // Logout is a MenuItem (triggers an action) rather than a FlyoutItem (navigates to a page)
        var logoutItem = new MenuItem
        {
            Text            = "Uitloggen",
            IconImageSource = ImageSource.FromFile("icon_logout.svg"),
            Command         = new Command(async () => await PerformLogoutAsync(authService))
        };
        Items.Add(logoutItem);

        Items.Add(MakeFlyoutItem("Mijn account",
            "icon_profile_white.svg",
            services.GetRequiredService<ProfilePage>(),
            "ProfilePage"));

        Items.Add(MakeFlyoutItem("Afspraken & reserveringen",
            "icon_bookmark_white.svg",
            services.GetRequiredService<MyLessonsPage>(),
            "MyLessonsPage"));

        Items.Add(MakeFlyoutItem("Rooster",
            "icon_calendar_white.svg",
            services.GetRequiredService<WeekPage>(),
            "WeekPage"));

        // ── Hidden items: navigable via //Route but not shown in the flyout ──

        // LessonsPage is opened from home screen tiles
        Items.Add(MakeHiddenFlyoutItem(
            services.GetRequiredService<LessonsPage>(), "LessonsPage"));

        // InstructorLessonsPage is opened from the instructor home screen tile
        Items.Add(MakeHiddenFlyoutItem(
            services.GetRequiredService<InstructorLessonsPage>(), "InstructorLessonsPage"));
    }

    // Creates a visible FlyoutItem with an icon, title, and a single content page.
    // The route string is used for programmatic navigation (GoToAsync("//RouteName")).
    private static FlyoutItem MakeFlyoutItem(
        string title, string iconFile, ContentPage page, string route)
    {
        var item = new FlyoutItem { Title = title };
        item.FlyoutIcon = ImageSource.FromFile(iconFile);
        var content = new ShellContent { Route = route, Content = page };
        item.Items.Add(content);
        return item;
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
    private static View BuildFlyoutHeader(IAuthenticationService authService)
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

        // Role badge in grey — maps internal role string to a friendly Dutch label
        var roleText = authService.CurrentUserRole?.ToLower() switch
        {
            "admin" or "employee" => "Administrator",
            "instructor"          => "Instructeur",
            _                     => "Lid"
        };
        var roleLabel = new Label
        {
            Text      = roleText,
            TextColor = Color.FromArgb("#9CA3AF"),
            FontSize  = 13
        };
        Grid.SetRow(roleLabel, 2);
        root.Children.Add(roleLabel);

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
            "Uitloggen",
            "Weet je zeker dat je wilt uitloggen?",
            "Ja, uitloggen",
            "Annuleren");

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
