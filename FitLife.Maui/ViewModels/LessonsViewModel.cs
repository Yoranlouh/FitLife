using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

public partial class LessonsViewModel : BaseViewModel
{
    private readonly ILessonService _lessonService;

    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    [ObservableProperty]
    private DateTime _currentDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _weekRangeText = string.Empty;

    [ObservableProperty]
    private string _selectedDateText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DayHeaderViewModel> _weekDays = new();

    private DateTime _startOfWeek;
    private IEnumerable<LessonResponse> _allLessons = [];

    public LessonsViewModel(ILessonService lessonService)
    {
        Title = "Lesaanbod";
        _lessonService = lessonService;
        UpdateWeekInfo();
    }

    private void UpdateWeekInfo()
    {
        var diff = (7 + ((int)CurrentDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
        _startOfWeek = CurrentDate.Date.AddDays(-diff);

        var weekNumber = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            _startOfWeek, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        WeekRangeText = $"Week {weekNumber}, {_startOfWeek:MMMM yyyy}";

        WeekDays.Clear();
        for (int i = 0; i < 7; i++)
        {
            var day = _startOfWeek.AddDays(i);
            WeekDays.Add(new DayHeaderViewModel
            {
                Date = day,
                DayName = day.ToString("ddd", new CultureInfo("nl-NL")).ToUpper().Replace(".", ""),
                DayNumber = day.Day.ToString(),
                IsSelected = day.Date == SelectedDate.Date
            });
        }

        UpdateSelectedDateText();
    }

    private void UpdateSelectedDateText()
    {
        SelectedDateText = SelectedDate.ToString("dddd, d MMM yyyy", new CultureInfo("nl-NL"));
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateWeekDaySelection();
        UpdateSelectedDateText();
        FilterLessonsForSelectedDay();
    }

    private void UpdateWeekDaySelection()
    {
        // Voorkom oneindige loops door direct de backing field te wijzigen
        foreach (var day in WeekDays)
        {
            var shouldBeSelected = day.Date.Date == SelectedDate.Date;
            if (day.IsSelected != shouldBeSelected)
            {
                day.IsSelected = shouldBeSelected;
            }
        }
    }

    /// <summary>
    /// Filter lessons collection to only show lessons for the selected day
    /// </summary>
    private void FilterLessonsForSelectedDay()
    {
        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: FilterLessonsForSelectedDay called. SelectedDate: {SelectedDate:yyyy-MM-dd}, Total lessons: {_allLessons.Count()}");

        Lessons.Clear();
        var filteredLessons = _allLessons
            .Where(l => l.StartTime.Date == SelectedDate.Date)
            .OrderBy(l => l.StartTime)
            .ToList();

        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: Found {filteredLessons.Count} lessons for {SelectedDate:yyyy-MM-dd}");

        foreach (var lesson in filteredLessons)
        {
            System.Diagnostics.Debug.WriteLine($"  - {lesson.StartTime:HH:mm} {lesson.WorkoutName} ({lesson.CurrentParticipantCount}/{lesson.MaxParticipants})");
            Lessons.Add(lesson);
        }

        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: Lessons.Count after filter: {Lessons.Count}");
    }

    /// <summary>
    /// Select a specific day and filter lessons for that day
    /// </summary>
    [RelayCommand]
    private void SelectDay(DateTime date)
    {
        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: SelectDay called with date: {date:yyyy-MM-dd}");
        SelectedDate = date;
        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: SelectedDate updated. Lessons count: {Lessons.Count}");
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        CurrentDate = CurrentDate.AddDays(-7);
        UpdateWeekInfo();
        SelectedDate = _startOfWeek; // Selecteer eerste dag van nieuwe week
        _ = LoadLessons();
    }

    [RelayCommand]
    private void NextWeek()
    {
        CurrentDate = CurrentDate.AddDays(7);
        UpdateWeekInfo();
        SelectedDate = _startOfWeek; // Selecteer eerste dag van nieuwe week
        _ = LoadLessons();
    }

    [RelayCommand]
    public async Task LoadLessons()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var lessons = await _lessonService.GetLessonsAsync();

            var endOfWeek = _startOfWeek.AddDays(7);
            _allLessons = lessons
                .Where(l => l.StartTime >= _startOfWeek && l.StartTime < endOfWeek)
                .OrderBy(l => l.StartTime)
                .ToList();

            FilterLessonsForSelectedDay();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Navigate to lesson detail page
    /// </summary>
    [RelayCommand]
    private async Task GoToDetails(LessonResponse lesson)
    {
        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: GoToDetails called with lesson: {lesson?.WorkoutName ?? "null"}");

        if (lesson == null)
        {
            System.Diagnostics.Debug.WriteLine("LessonsViewModel: Lesson is null, cannot navigate");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"LessonsViewModel: Navigating to LessonDetailPage for lesson ID: {lesson.Id}");
            await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
            {
                { "Lesson", lesson }
            });
            System.Diagnostics.Debug.WriteLine("LessonsViewModel: Navigation completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LessonsViewModel: Navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GoToWeekView()
    {
        await Shell.Current.GoToAsync("//WeekPage");
    }
}
