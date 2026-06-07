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
    private readonly IAuthenticationService _authenticationService;
    private readonly ILessonManagementService _managementService;

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

    // Shown below the grid when no lessons exist for this week or when an error occurred
    [ObservableProperty]
    private string _emptyMessage = string.Empty;

    // Lessons filtered to only the selected day — displayed in the lesson list below the calendar
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _selectedDayLessons = new();

    // The Monday of the currently displayed week — used to slice the lesson list
    private DateTime _startOfWeek;

    // Full lesson collection from the API (kept in memory to avoid re-fetching when switching days)
    private IEnumerable<LessonResponse> _allLessons = [];

    // All workouts from the workouts table — loaded once and reused for the legend.
    // Populated independently of which week is shown so the legend is always complete.
    private List<WorkoutLegendItem> _allWorkouts = [];

    // Incremented every time LoadLessons starts. If a newer load has started by the time
    // an older one's await returns, the stale result is discarded so the grid never shows
    // data for the wrong week (e.g. when pressing ◀/▶ during a slow API call).
    private int _loadVersion;

    public WeekViewModel(ILessonService lessonService,
                         IAuthenticationService authenticationService,
                         ILessonManagementService managementService)
    {
        _lessonService     = lessonService;
        _authenticationService = authenticationService;
        _managementService = managementService;
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

        // ISOWeek is locale-agnostic (no dependency on the device's calendar system).
        // CultureInfo.CurrentCulture.Calendar.GetWeekOfYear crashes on non-Gregorian
        // calendars (Hebrew, Hijri, etc.) when CalendarWeekRule.FirstFourDayWeek is used.
        var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(_startOfWeek);
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
    private async Task PreviousWeek()
    {
        CurrentDate = CurrentDate.AddDays(-7);
        UpdateWeekInfo();
        await LoadLessons();
    }

    // Moves the week view forward by 7 days and reloads lessons from the API.
    [RelayCommand]
    private async Task NextWeek()
    {
        CurrentDate = CurrentDate.AddDays(7);
        UpdateWeekInfo();
        await LoadLessons();
    }

    // Fetches only the current week's lessons from the API (server-side date filter).
    // Workouts are NOT loaded here — they are loaded lazily on first legend open so
    // a slow workouts endpoint can never block the calendar from appearing.
    // Uses a version counter so stale results from cancelled navigations are discarded.
    // AllowConcurrentExecutions = true so re-navigating while a slow lessons call is
    // still in flight triggers a fresh load rather than silently doing nothing.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadLessons()
    {
        var version = ++_loadVersion;
        IsBusy = true;
        EmptyMessage = string.Empty;
        try
        {
            var endOfWeek = _startOfWeek.AddDays(7);

            var lessons = await _lessonService.GetLessonsAsync(
                _authenticationService.CurrentUserId,
                from: _startOfWeek,
                to: endOfWeek);

            // A newer navigation happened while we were awaiting — discard stale results
            if (version != _loadVersion) return;

            _allLessons = lessons.OrderBy(l => l.StartTime).ToList();

            Lessons.Clear();
            foreach (var lesson in _allLessons)
                Lessons.Add(lesson);

            if (!_allLessons.Any())
                EmptyMessage = "Geen lessen gepland voor deze week.";

            FilterLessonsForSelectedDay();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeekViewModel] Fout bij laden lessen: {ex.Message}");
            if (version == _loadVersion)
                EmptyMessage = "Lessen konden niet worden geladen. Controleer de verbinding.";
        }
        finally
        {
            if (version == _loadVersion)
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

    // Opens the colour legend popup. Workouts are fetched lazily here (not during
    // LoadLessons) so a slow workouts endpoint never delays the calendar grid.
    // After the first successful fetch the result is cached in _allWorkouts.
    [RelayCommand]
    private async Task ShowLegend()
    {
        if (_allWorkouts.Count == 0)
        {
            try
            {
                var workouts = await _managementService.GetWorkoutsAsync();
                _allWorkouts = workouts
                    .Select(w => new WorkoutLegendItem(w.Name, w.Color ?? "#5B6636"))
                    .OrderBy(i => i.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WeekViewModel] Workouts fout: {ex.Message}");
            }
        }
        var popup = new Views.LegendPopup(_allWorkouts);
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
