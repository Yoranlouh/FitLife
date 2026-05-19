using CommunityToolkit.Maui.Views;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.Views;

public partial class MultipleLessonsPopup : Popup
{
    public MultipleLessonsPopup(IEnumerable<LessonResponse> lessons)
    {
        InitializeComponent();
        
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
