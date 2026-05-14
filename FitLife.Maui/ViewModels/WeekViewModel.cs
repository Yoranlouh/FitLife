using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using System.Globalization;

namespace FitLife.Maui.ViewModels;

public partial class WeekViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    [ObservableProperty]
    private DateTime _currentDate = DateTime.Today;

    [ObservableProperty]
    private string _weekRangeText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DayHeaderViewModel> _weekDays = new();

    public WeekViewModel()
    {
        Title = "Weekoverzicht";
        UpdateWeekInfo();
    }

    private void UpdateWeekInfo()
    {
        var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var diff = (7 + (CurrentDate.Date.DayOfWeek - firstDayOfWeek)) % 7;
        var startOfWeek = CurrentDate.Date.AddDays(-1 * diff);

        var weekNumber = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(startOfWeek, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        WeekRangeText = $"Week {weekNumber}, {startOfWeek:MMMM yyyy}";

        WeekDays.Clear();
        for (int i = 0; i < 7; i++)
        {
            var day = startOfWeek.AddDays(i);
            WeekDays.Add(new DayHeaderViewModel 
            { 
                DayName = day.ToString("ddd", CultureInfo.CurrentCulture).Substring(0, 2).ToLower(), 
                DayNumber = day.Day.ToString("00") 
            });
        }

        LoadLessons();
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        CurrentDate = CurrentDate.AddDays(-7);
        UpdateWeekInfo();
    }

    [RelayCommand]
    private void NextWeek()
    {
        CurrentDate = CurrentDate.AddDays(7);
        UpdateWeekInfo();
    }

    [RelayCommand]
    private void LoadLessons()
    {
        // Mock data
        Lessons.Clear();
        
        var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var diff = (7 + (CurrentDate.Date.DayOfWeek - firstDayOfWeek)) % 7;
        var startOfWeek = CurrentDate.Date.AddDays(-1 * diff);

        // Add some mock lessons matching the image style
        Lessons.Add(new LessonResponse { WorkoutName = "W1", StartTime = startOfWeek.AddDays(2).AddHours(6), InstructorName = "Jan", LocationName = "Zaal 1" }); // Wo 6:00
        Lessons.Add(new LessonResponse { WorkoutName = "W2", StartTime = startOfWeek.AddDays(4).AddHours(6), InstructorName = "Piet", LocationName = "Zaal 2" }); // Vr 6:00
        Lessons.Add(new LessonResponse { WorkoutName = "W7", StartTime = startOfWeek.AddDays(2).AddHours(8), InstructorName = "Jan", LocationName = "Zaal 1" }); // Wo 8:00
        Lessons.Add(new LessonResponse { WorkoutName = "W0", StartTime = startOfWeek.AddDays(4).AddHours(8), InstructorName = "Piet", LocationName = "Zaal 2" }); // Vr 8:00
        
        // Add more mock data as needed to match the image
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

public class DayHeaderViewModel
{
    public string DayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
}
