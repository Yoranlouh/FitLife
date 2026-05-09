using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;

namespace FitLife.Maui.ViewModels;

public partial class DayViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    public DayViewModel()
    {
        Title = "Dagoverzicht";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("date", out var dateObj) && dateObj is string dateStr && DateTime.TryParse(dateStr, out var date))
        {
            SelectedDate = date;
            LoadLessons();
        }
    }

    [RelayCommand]
    private void LoadLessons()
    {
        // Mock data gefilterd op datum
        Lessons.Clear();
        Lessons.Add(new LessonResponse { Id = 1, WorkoutName = "Kracht", StartTime = SelectedDate.AddHours(10), InstructorName = "Henk", LocationName = "Zaal 1" });
    }

    [RelayCommand]
    private async Task GoToDetails(LessonResponse lesson)
    {
        await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }
}
