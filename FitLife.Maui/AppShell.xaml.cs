namespace FitLife.Maui;

/// <summary>
/// Main Shell for authenticated app navigation
/// Contains TabBar with main pages and registers routes for detail pages
/// </summary>
public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for detail/secondary pages that are pushed onto navigation stack
		// SplashPage and LoginPage are NOT registered here - they live outside Shell
		Routing.RegisterRoute("DayPage", typeof(Views.DayPage));
		Routing.RegisterRoute("LessonDetailPage", typeof(Views.LessonDetailPage));
		Routing.RegisterRoute("ParticipantsPage", typeof(Views.ParticipantsPage));
		Routing.RegisterRoute("SubscriptionPage", typeof(Views.SubscriptionPage));
		Routing.RegisterRoute("MyLessonsPage", typeof(Views.MyLessonsPage));
	}
}
