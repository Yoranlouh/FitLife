using FitLife.Maui.Services;
using Microsoft.Extensions.Logging;
using FitLife.Maui.ViewModels;
using FitLife.Maui.Views;
using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace FitLife.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		try
		{
			var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif

        // Services
        builder.Services.AddHttpClient<ILessonService, LessonService>(client =>
        {
            // Docker API draait op poort 8080.
            // Android emulator gebruikt 10.0.2.2 om de hostmachine te bereiken.
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080/"
                : "http://localhost:8080/";

            client.BaseAddress = new Uri(baseUrl);
        });

        builder.Services.AddHttpClient<IParticipantService, ParticipantService>(client =>
        {
            // Docker API draait op poort 8080.
            // Android emulator gebruikt 10.0.2.2 om de hostmachine te bereiken.
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080/"
                : "http://localhost:8080/";

            client.BaseAddress = new Uri(baseUrl);
        });

        // ViewModels - Use Transient for pages that need fresh state on each navigation
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

		// Pages - Match ViewModel lifecycle
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

			return builder.Build();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"STARTUP ERROR: {ex}");
			throw;
		}
	}
}
