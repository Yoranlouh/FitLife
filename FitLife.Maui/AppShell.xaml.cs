using FitLife.Maui.Controls;
using FitLife.Maui.Services;
using FitLife.Maui.Views;

namespace FitLife.Maui;

public partial class AppShell : Shell
{
    public AppShell(IAuthenticationService authService, IServiceProvider services)
    {
        InitializeComponent();

        // Push-routes: niet in flyout, maar wel via GoToAsync("RouteName") bereikbaar
        Routing.RegisterRoute("DayPage",             typeof(DayPage));
        Routing.RegisterRoute("LessonDetailPage",    typeof(LessonDetailPage));
        Routing.RegisterRoute("ParticipantsPage",    typeof(ParticipantsPage));
        Routing.RegisterRoute("SubscriptionPage",    typeof(SubscriptionPage));
        Routing.RegisterRoute("ManageLessonPage",    typeof(ManageLessonPage));
        Routing.RegisterRoute("SettingsPage",        typeof(SettingsPage));
        Routing.RegisterRoute("MySportclubPage",     typeof(MySportclubPage));

        BuildFlyout(authService, services);
    }

    // ── Flyout opbouw ──────────────────────────────────────────────────────

    private void BuildFlyout(IAuthenticationService authService, IServiceProvider services)
    {
        // Profiel-header bovenaan het flyout
        FlyoutHeader         = BuildFlyoutHeader(authService);
        FlyoutHeaderBehavior = FlyoutHeaderBehavior.Fixed;

        // ── Zichtbare menu-items (exact gevraagde volgorde) ────────────────

        // 1. Home
        Items.Add(MakeFlyoutItem("Home",
            "icon_home.svg",
            services.GetRequiredService<HomePage>(),
            "HomePage"));

        // 2. Instellingen
        Items.Add(MakeFlyoutItem("Instellingen",
            "icon_settings.svg",
            services.GetRequiredService<SettingsPage>(),
            "SettingsPage"));

        // 3. Mijn sportclub
        Items.Add(MakeFlyoutItem("Mijn sportclub",
            "icon_sportclub.svg",
            services.GetRequiredService<MySportclubPage>(),
            "MySportclubPage"));

        // 4. Uitloggen (MenuItem — triggert actie, geen navigatie)
        var logoutItem = new MenuItem
        {
            Text             = "Uitloggen",
            IconImageSource  = ImageSource.FromFile("icon_logout.svg"),
            Command          = new Command(async () => await PerformLogoutAsync(authService))
        };
        Items.Add(logoutItem);

        // 5. Mijn account
        Items.Add(MakeFlyoutItem("Mijn account",
            "icon_profile.svg",
            services.GetRequiredService<ProfilePage>(),
            "ProfilePage"));

        // 6. Afspraken & reserveringen
        Items.Add(MakeFlyoutItem("Afspraken & reserveringen",
            "icon_bookmark.svg",
            services.GetRequiredService<MyLessonsPage>(),
            "MyLessonsPage"));

        // 7. Rooster
        Items.Add(MakeFlyoutItem("Rooster",
            "icon_calendar.svg",
            services.GetRequiredService<WeekPage>(),
            "WeekPage"));

        // ── Verborgen items: routes beschikbaar via //Route maar niet in flyout ──

        // LessonsPage (bereikbaar via tegels op homepage)
        Items.Add(MakeHiddenFlyoutItem(
            services.GetRequiredService<LessonsPage>(), "LessonsPage"));

        // InstructorLessonsPage (bereikbaar via tegels voor instructeurs)
        Items.Add(MakeHiddenFlyoutItem(
            services.GetRequiredService<InstructorLessonsPage>(), "InstructorLessonsPage"));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static FlyoutItem MakeFlyoutItem(
        string title, string iconFile, ContentPage page, string route)
    {
        var item = new FlyoutItem { Title = title };
        item.FlyoutIcon = ImageSource.FromFile(iconFile);
        var content = new ShellContent { Route = route, Content = page };
        item.Items.Add(content);
        return item;
    }

    private static FlyoutItem MakeHiddenFlyoutItem(ContentPage page, string route)
    {
        var item = new FlyoutItem { Title = route };
        Shell.SetFlyoutItemIsVisible(item, false);
        var content = new ShellContent { Route = route, Content = page };
        item.Items.Add(content);
        return item;
    }

    // ── Flyout header (profielfoto + naam + rol) ───────────────────────────

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

        // Avatar
        var avatar = new UserAvatarView
        {
            PhotoUrl          = authService.CurrentUserPhotoUrl,
            AvatarSize        = 72,
            HorizontalOptions = LayoutOptions.Start
        };
        Grid.SetRow(avatar, 0);
        root.Children.Add(avatar);

        // Gebruikersnaam
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

        // Accounttype / rol
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

    // ── Logout ─────────────────────────────────────────────────────────────

    private static async Task PerformLogoutAsync(IAuthenticationService authService)
    {
        // Sluit het flyout eerst
        Current.FlyoutIsPresented = false;

        var confirmed = await Current.DisplayAlert(
            "Uitloggen",
            "Weet je zeker dat je wilt uitloggen?",
            "Ja, uitloggen",
            "Annuleren");

        if (!confirmed) return;

        await authService.LogoutAsync();

        // Vervang de gehele app-shell door de login-pagina (zelfde patroon als ProfileViewModel)
        var appServices = Application.Current?.Handler?.MauiContext?.Services;
        if (appServices != null)
        {
            var loginPage = appServices.GetService<Views.LoginPage>();
            if (loginPage != null && Application.Current != null)
            {
                Application.Current.MainPage = loginPage;
                return;
            }
        }

        // Fallback
        if (Application.Current != null)
            Application.Current.MainPage = new Views.SplashPage();
    }
}