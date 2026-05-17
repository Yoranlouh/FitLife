namespace FitLife.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("DayPage", typeof(Views.DayPage));
		Routing.RegisterRoute("LessonDetailPage", typeof(Views.LessonDetailPage));
		Routing.RegisterRoute("ParticipantsPage", typeof(Views.ParticipantsPage));
		Routing.RegisterRoute("SubscriptionPage", typeof(Views.SubscriptionPage));
		Routing.RegisterRoute("MyLessonsPage", typeof(Views.MyLessonsPage));
	}
}
