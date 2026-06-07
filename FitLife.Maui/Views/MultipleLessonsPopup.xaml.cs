using CommunityToolkit.Maui.Views;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.Views;

public partial class MultipleLessonsPopup : Popup<LessonResponse>
{
    public MultipleLessonsPopup(IEnumerable<LessonResponse> lessons)
    {
        InitializeComponent();
        
        LessonsListView.ItemsSource = lessons;
    }

    private async void OnLessonTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is LessonResponse lesson)
        {
            await CloseAsync(lesson);
        }
    }
}
