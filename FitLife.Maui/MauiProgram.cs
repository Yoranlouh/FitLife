using FitLife.Maui.Services;
using Microsoft.Extensions.Logging;
using FitLife.Maui.ViewModels;
using FitLife.Maui.Views;
using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace FitLife.Maui;

// Entry point for the MAUI application.
// Configures dependency injection (DI), HTTP clients, and the page/ViewModel graph.
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()  // enables CommunityToolkit controls (e.g. Popup)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf",   "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf",  "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif

            // ── HTTP Services ─────────────────────────────────────────────────────
            // AddHttpClient<TInterface, TImpl> registers a typed HTTP client with a
            // pre-configured BaseAddress. Android emulator uses 10.0.2.2 to reach the host.

            builder.Services.AddHttpClient<ILessonService, LessonService>(client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });

            builder.Services.AddHttpClient<IParticipantService, ParticipantService>(client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });

            builder.Services.AddHttpClient<ISubscriptionService, SubscriptionService>(client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });

            // The NotificationService is a Singleton (shared state across all pages).
            // It requires a named HttpClient and the IAuthenticationService — both
            // are wired up manually via the factory overload of AddSingleton.
            builder.Services.AddHttpClient("NotificationClient", client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });
            builder.Services.AddSingleton<INotificationService>(sp =>
            {
                var httpClient  = sp.GetRequiredService<IHttpClientFactory>().CreateClient("NotificationClient");
                var authService = sp.GetRequiredService<IAuthenticationService>();
                return new NotificationService(httpClient, authService);
            });

            builder.Services.AddHttpClient<IReservationService, ReservationService>(client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });

            builder.Services.AddHttpClient<ILessonManagementService, LessonManagementService>(client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });

            // AuthenticationService is Singleton — it holds the logged-in user's session data.
            // A named HttpClient is used so the base URL can be configured independently.
            builder.Services.AddSingleton<IAuthenticationService>(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient        = httpClientFactory.CreateClient(nameof(AuthenticationService));

                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                httpClient.BaseAddress = new Uri(baseUrl);

                return new AuthenticationService(httpClient);
            });
            builder.Services.AddHttpClient(nameof(AuthenticationService));

            builder.Services.AddHttpClient<IPhotoService, PhotoService>(client =>
            {
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:8080/"
                    : "http://localhost:8080/";
                client.BaseAddress = new Uri(baseUrl);
            });

            // ── ViewModels ─────────────────────────────────────────────────────────
            // Transient = new instance created for every navigation (fresh state).
            // Singleton = shared instance, state persists across all navigations.

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddTransient<LessonsViewModel>();
            builder.Services.AddSingleton<ProfileViewModel>();
            builder.Services.AddSingleton<SubscriptionViewModel>();
            builder.Services.AddSingleton<MyLessonsViewModel>();
            builder.Services.AddSingleton<InstructorParticipantListViewModel>();
            builder.Services.AddTransient<WeekViewModel>();
            builder.Services.AddTransient<DayViewModel>();
            builder.Services.AddTransient<LessonDetailViewModel>();
            builder.Services.AddTransient<ParticipantsViewModel>();
            builder.Services.AddSingleton<InstructorLessonsViewModel>();
            builder.Services.AddTransient<ManageLessonViewModel>();
            builder.Services.AddTransient<NotificationsViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();

            // ── Pages ──────────────────────────────────────────────────────────────
            // Lifetime matches the ViewModel so page and VM are created/destroyed together.

            builder.Services.AddTransient<SplashPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddTransient<LessonsPage>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddSingleton<SubscriptionPage>();
            builder.Services.AddSingleton<MyLessonsPage>();
            builder.Services.AddSingleton<InstructorParticipantListPage>();
            builder.Services.AddTransient<WeekPage>();
            builder.Services.AddTransient<DayPage>();
            builder.Services.AddTransient<LessonDetailPage>();
            builder.Services.AddTransient<ParticipantsPage>();
            builder.Services.AddSingleton<InstructorLessonsPage>();
            builder.Services.AddTransient<ManageLessonPage>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddSingleton<MySportclubPage>();
            builder.Services.AddTransient<NotificationsPage>();

            // AppShell is Transient so it is rebuilt with the correct role-based flyout
            // each time the user logs in.
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"STARTUP ERROR: {ex}");
            throw;
        }
    }
}
