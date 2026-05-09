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
#endif

		// ViewModels
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<LessonsViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<SubscriptionViewModel>();
		builder.Services.AddTransient<MyLessonsViewModel>();
		builder.Services.AddTransient<InstructorParticipantListViewModel>();
		builder.Services.AddTransient<WeekViewModel>();
		builder.Services.AddTransient<DayViewModel>();
		builder.Services.AddTransient<LessonDetailViewModel>();

		// Pages
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<LessonsPage>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<SubscriptionPage>();
		builder.Services.AddTransient<MyLessonsPage>();
		builder.Services.AddTransient<InstructorParticipantListPage>();
		builder.Services.AddTransient<WeekPage>();
		builder.Services.AddTransient<DayPage>();
		builder.Services.AddTransient<LessonDetailPage>();

		return builder.Build();
	}
}
