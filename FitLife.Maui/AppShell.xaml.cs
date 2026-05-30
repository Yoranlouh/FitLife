using FitLife.Maui.Services;
using FitLife.Maui.Views;

namespace FitLife.Maui;

public partial class AppShell : Shell
{
    public AppShell(IAuthenticationService authService, IServiceProvider services)
    {
        InitializeComponent();

        Routing.RegisterRoute("DayPage", typeof(DayPage));
        Routing.RegisterRoute("LessonDetailPage", typeof(LessonDetailPage));
        Routing.RegisterRoute("ParticipantsPage", typeof(ParticipantsPage));
        Routing.RegisterRoute("SubscriptionPage", typeof(SubscriptionPage));
        Routing.RegisterRoute("ManageLessonPage", typeof(ManageLessonPage));
        Routing.RegisterRoute("WeekPage", typeof(WeekPage));

        BuildTabs(authService, services);
    }

    private void BuildTabs(IAuthenticationService authService, IServiceProvider services)
    {
        var tabBar = new TabBar();

        // Home tab - always visible
        tabBar.Items.Add(MakeTab("Home", "🏠", services.GetRequiredService<HomePage>(), "HomePage"));

        if (authService.IsAdmin)
        {
            tabBar.Items.Add(MakeTab("Lessen", "📅", services.GetRequiredService<LessonsPage>(), "LessonsPage"));
            tabBar.Items.Add(MakeTab("Profiel", "👤", services.GetRequiredService<ProfilePage>(), "ProfilePage"));
        }
        else if (authService.IsInstructor)
        {
            tabBar.Items.Add(MakeTab("Geven", "📋", services.GetRequiredService<InstructorLessonsPage>(), "InstructorLessonsPage"));
            tabBar.Items.Add(MakeTab("Deelnemen", "🔖", services.GetRequiredService<MyLessonsPage>(), "MyLessonsPage"));
            tabBar.Items.Add(MakeTab("Profiel", "👤", services.GetRequiredService<ProfilePage>(), "ProfilePage"));
        }
        else
        {
            // Member
            tabBar.Items.Add(MakeTab("Week", "📆", services.GetRequiredService<WeekPage>(), "WeekPage"));
            tabBar.Items.Add(MakeTab("Lessen", "📅", services.GetRequiredService<LessonsPage>(), "LessonsPage"));
            tabBar.Items.Add(MakeTab("Mijn Lessen", "🔖", services.GetRequiredService<MyLessonsPage>(), "MyLessonsPage"));
            tabBar.Items.Add(MakeTab("Profiel", "👤", services.GetRequiredService<ProfilePage>(), "ProfilePage"));
        }

        Items.Add(tabBar);
    }

    private static Tab MakeTab(string title, string icon, ContentPage page, string route)
    {
        var content = new ShellContent
        {
            Title = title,
            Content = page,
            Route = route
        };
        return new Tab
        {
            Title = title,
            Items = { content }
        };
    }
}