using CommunityToolkit.Maui.Views;
using SharedLibrary.DTOs.Responses;
using FitLife.Maui.Services; // <-- VOEG DEZE TOE (of waar IParticipantService staat)

namespace FitLife.Maui.Views;

public partial class MultipleLessonsPopup : Popup
{
    private readonly IParticipantService? _participantService;

    public MultipleLessonsPopup(IEnumerable<LessonResponse> lessons)
    {
        InitializeComponent();
        
        _participantService = IPlatformApplication.Current?.Services.GetService<IParticipantService>();
        
        LessonsListView.ItemsSource = lessons;
    }

    private void OnLessonTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is LessonResponse lesson)
        {
            Close(lesson);
        }
    }
}
