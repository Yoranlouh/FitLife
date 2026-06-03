using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using System.Globalization;
using CommunityToolkit.Maui.Views;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Week schedule page.
// Shows all lessons for the current week in a horizontal day-strip calendar.
// The user can tap a day header to filter the lesson list to that day,
// or swipe to the previous/next week.
public partial class WeekViewModel : BaseViewModel
{
    private readonly ILessonService _lessonService;

    // All lessons for the currently visible week
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    // The reference date used to calculate which week is shown
    [ObservableProperty]
    private DateTime _currentDate = DateTime.Today;

    // Human-readable header, e.g. "Week 24, juni 2024"
    [ObservableProperty]
    private string _weekRangeText = string.Empty;

    // The seven day-header view models shown at the top of the page
    [ObservableProperty]
    private ObservableCollection<DayHeaderViewModel> _weekDays = new();

    // Which day is currently highlighted/selected in the day strip
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    // Lessons filtered to only the selected day — displayed in the lesson list below the calendar
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _selectedDayLessons = new();

    // The Monday of the currently displayed week — used to slice the lesson list
    private DateTime _startOfWeek;

    // Full lesson collection from the API (kept in memory to avoid re-fetching when switching days)
    private IEnumerable<LessonResponse> _allLessons = [];

    public WeekViewModel(ILessonService lessonService)
    {
        _lessonService = lessonService;
        Title = "Weekoverzicht";
        UpdateWeekInfo();  // initialise day headers and week label for today's week
    }

    // Recalculates the week label and rebuilds the seven DayHeaderViewModel objects
    // whenever CurrentDate changes (e.g. after PreviousWeek / NextWeek).
    private void UpdateWeekInfo()
    {
        // Calculate the offset from CurrentDate back to the most recent Monday
        var diff     = (7 + ((int)CurrentDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
        _startOfWeek = CurrentDate.Date.AddDays(-diff);

        // ISO week number (first week with ≥4 days in the new year)
        var weekNumber = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            _startOfWeek, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        WeekRangeText = $"Week {weekNumber}, {_startOfWeek:MMMM yyyy}";

        // Rebuild the day-strip headers (Mon through Sun)
        WeekDays.Clear();
        for (int i = 0; i < 7; i++)
        {
            var day = _startOfWeek.AddDays(i);
            WeekDays.Add(new DayHeaderViewModel
            {
                Date      = day,
                DayName   = day.ToString("ddd", new CultureInfo("nl-NL")).ToUpper().Replace(".", ""),
                DayNumber = day.Day.ToString(),
                IsSelected = day.Date == SelectedDate.Date
            });
        }
    }

    // Called automatically by CommunityToolkit when SelectedDate changes.
    // Updates the visual selection state and re-filters the lesson list.
    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateWeekDaySelection();
        FilterLessonsForSelectedDay();
    }

    // Updates each DayHeaderViewModel's IsSelected flag to reflect the new selection.
    // Only mutates the flag when it differs to avoid redundant UI updates.
    private void UpdateWeekDaySelection()
    {
        foreach (var day in WeekDays)
        {
            var shouldBeSelected = day.Date.Date == SelectedDate.Date;
            if (day.IsSelected != shouldBeSelected)
                day.IsSelected = shouldBeSelected;
        }
    }

    // Copies the lessons that fall on SelectedDate from the full list into SelectedDayLessons.
    private void FilterLessonsForSelectedDay()
    {
        SelectedDayLessons.Clear();
        foreach (var lesson in Lessons.Where(l => l.StartTime.Date == SelectedDate.Date)
                                      .OrderBy(l => l.StartTime))
        {
            SelectedDayLessons.Add(lesson);
        }
    }

    // Command bound to each day-header tap — sets the selected day.
    [RelayCommand]
    private void SelectDay(DateTime date)
    {
        SelectedDate = date;
    }

    // Moves the week view back by 7 days and reloads lessons from the API.
    [RelayCommand]
    private void PreviousWeek()
    {
        CurrentDate = CurrentDate.AddDays(-7);
        UpdateWeekInfo();
        _ = LoadLessons();
    }

    // Moves the week view forward by 7 days and reloads lessons from the API.
    [RelayCommand]
    private void NextWeek()
    {
        CurrentDate = CurrentDate.AddDays(7);
        UpdateWeekInfo();
        _ = LoadLessons();
    }

    // Fetches all lessons from the API, filters them to the current week window,
    // and populates the Lessons collection. Then re-filters for the selected day.
    [RelayCommand]
    private async Task LoadLessons()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var lessons = await _lessonService.GetLessonsAsync();
            _allLessons = lessons.ToList();

            // Only show lessons within the Mon–Sun window of the current week
            var endOfWeek       = _startOfWeek.AddDays(7);
            var filteredLessons = _allLessons
                .Where(l => l.StartTime >= _startOfWeek && l.StartTime < endOfWeek)
                .OrderBy(l => l.StartTime)
                .ToList();

            Lessons.Clear();
            foreach (var lesson in filteredLessons)
                Lessons.Add(lesson);

            FilterLessonsForSelectedDay();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading lessons: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Navigates to the full-day view for the tapped date.
    [RelayCommand]
    private async Task GoToDayView(DateTime date)
    {
        await Shell.Current.GoToAsync($"DayPage?date={date:yyyy-MM-dd}");
    }

    // Navigates to the lesson detail page, passing the full lesson object.
    [RelayCommand]
    public async Task GoToDetails(LessonResponse lesson)
    {
        await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }

    // Opens a popup showing the colour legend (one entry per workout type in the current week).
    [RelayCommand]
    private async Task ShowLegend()
    {
        var items = Lessons
            .GroupBy(l => l.WorkoutName)
            .Select(g => new WorkoutLegendItem(g.Key, g.First().WorkoutColor))
            .OrderBy(i => i.Name)
            .ToList();

        var popup = new Views.LegendPopup(items);
        await Shell.Current.CurrentPage.ShowPopupAsync(popup);
    }
}

// Represents one column header in the weekly day strip (e.g. "MA" / "12").
// IsSelected is observable so the selected highlight updates without rebuilding the list.
public partial class DayHeaderViewModel : ObservableObject
{
    public DateTime Date      { get; set; }
    public string   DayName   { get; set; } = string.Empty;  // abbreviated day name, e.g. "MA"
    public string   DayNumber { get; set; } = string.Empty;  // day-of-month digit, e.g. "12"

    [ObservableProperty]
    private bool _isSelected;
}

// Immutable record used by the colour legend popup — one entry per distinct workout type.
public record WorkoutLegendItem(string Name, string Color);
