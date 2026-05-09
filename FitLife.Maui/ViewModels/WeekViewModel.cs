using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

public partial class WeekViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    public WeekViewModel()
    {
        Title = "Weekoverzicht";
        LoadLessons();
    }

    [RelayCommand]
    private void LoadLessons()
    {
        // Mock data voor nu
        Lessons.Clear();
        Lessons.Add(new LessonResponse { Id = 1, WorkoutName = "Yoga", StartTime = DateTime.Now.AddHours(2), InstructorName = "Jan", LocationName = "Zaal 1" });
        Lessons.Add(new LessonResponse { Id = 2, WorkoutName = "Spinning", StartTime = DateTime.Now.AddDays(1).AddHours(1), InstructorName = "Piet", LocationName = "Zaal 2" });
    }

    [RelayCommand]
    private async Task GoToDayView(DateTime date)
    {
        await Shell.Current.GoToAsync($"DayPage?date={date:yyyy-MM-dd}");
    }

    [RelayCommand]
    private async Task GoToDetails(LessonResponse lesson)
    {
        await Shell.Current.GoToAsync($"LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }
}
