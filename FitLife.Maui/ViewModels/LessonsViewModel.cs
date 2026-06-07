using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

// ViewModel for the main lesson schedule page.
// Shows a weekly day-strip calendar at the top and a filtered lesson list below.
// Fetches all lessons from the API and filters client-side to the selected day
// so navigating between days is instant (no extra API call needed).
public partial class LessonsViewModel : BaseViewModel
{
    private readonly ILessonService _lessonService;
    private readonly IAuthenticationService _authenticationService;

    // The lessons visible in the list (filtered to the selected day)
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    // The reference date for which week is shown in the calendar strip
    [ObservableProperty]
    private DateTime _currentDate = DateTime.Today;

    // The day whose lessons are shown in the list below the calendar
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    // E.g. "Week 24, juni 2024" — displayed in the page header
    [ObservableProperty]
    private string _weekRangeText = string.Empty;

    // E.g. "maandag, 10 jun 2024" — displayed above the lesson list
    [ObservableProperty]
    private string _selectedDateText = string.Empty;

    // The seven day-header items shown in the horizontal calendar strip
    [ObservableProperty]
    private ObservableCollection<DayHeaderViewModel> _weekDays = new();

    // Tracks which lesson the user tapped — used to navigate to the detail page
    [ObservableProperty]
    private LessonResponse? _selectedLesson;

    private DateTime _startOfWeek;

    // All lessons for the current week, kept in memory to avoid re-fetching on day change
    private IEnumerable<LessonResponse> _allLessons = [];

    public LessonsViewModel(ILessonService lessonService, IAuthenticationService authenticationService)
    {
        Title                   = "Lesaanbod";
        _lessonService          = lessonService;
        _authenticationService  = authenticationService;
        UpdateWeekInfo();  // build the initial day strip for today's week
    }

    // Calculates _startOfWeek, updates WeekRangeText, and rebuilds the seven DayHeaderViewModel items.
    private void UpdateWeekInfo()
    {
        var diff     = (7 + ((int)CurrentDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
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
                Date      = day,
                DayName   = day.ToString("ddd", new CultureInfo("nl-NL")).ToUpper().Replace(".", ""),
                DayNumber = day.Day.ToString(),
                IsSelected = day.Date == SelectedDate.Date
            });
        }

        UpdateSelectedDateText();
    }

    // Formats the selected date as a full Dutch string, e.g. "maandag, 10 jun 2024"
    private void UpdateSelectedDateText()
    {
        SelectedDateText = SelectedDate.ToString("dddd, d MMM yyyy", new CultureInfo("nl-NL"));
    }

    // Auto-called when SelectedDate changes — refreshes day strip highlights and lesson list
    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateWeekDaySelection();
        UpdateSelectedDateText();
        FilterLessonsForSelectedDay();
    }

    // Syncs each DayHeaderViewModel's IsSelected flag with the new SelectedDate
    private void UpdateWeekDaySelection()
    {
        foreach (var day in WeekDays)
        {
            var shouldBeSelected = day.Date.Date == SelectedDate.Date;
            if (day.IsSelected != shouldBeSelected)
                day.IsSelected = shouldBeSelected;
        }
    }

    // Copies lessons that match the selected date from _allLessons into the Lessons collection
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
    }

    // Command bound to each day header tap — changes SelectedDate if the tapped day differs
    [RelayCommand]
    private void SelectDay(object dateParam)
    {
        DateTime date;
        if (dateParam is DateTime dt)
            date = dt;
        else if (dateParam != null && DateTime.TryParse(dateParam.ToString(), out var parsedDate))
            date = parsedDate;
        else
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] LessonsViewModel: SelectDay called with invalid parameter type: {dateParam?.GetType().Name}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] LessonsViewModel: SelectDay called with date: {date:yyyy-MM-dd}");
        if (SelectedDate.Date != date.Date)
            SelectedDate = date;
        else
        {
            // Already on this day — re-apply the filter in case the list was cleared
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] LessonsViewModel: SelectedDate was already {date:yyyy-MM-dd}, still filtering to be sure");
            FilterLessonsForSelectedDay();
        }
    }

    // Moves one week back, rebuilds the day strip, and reloads lessons from the API
    [RelayCommand]
    private async Task PreviousWeek()
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] LessonsViewModel: PreviousWeek called");
        CurrentDate  = CurrentDate.AddDays(-7);
        UpdateWeekInfo();
        SelectedDate = _startOfWeek;  // jump to the first day of the new week
        await LoadLessons();
    }

    // Moves one week forward, rebuilds the day strip, and reloads lessons from the API
    [RelayCommand]
    private async Task NextWeek()
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] LessonsViewModel: NextWeek called");
        CurrentDate  = CurrentDate.AddDays(7);
        UpdateWeekInfo();
        SelectedDate = _startOfWeek;
        await LoadLessons();
    }

    // Fetches all lessons from GET /lessons, filters to the current week, and stores in _allLessons.
    // Then calls FilterLessonsForSelectedDay() to update the visible list.
    [RelayCommand]
    private async Task LoadLessons()
    {
        if (IsBusy) return;

        System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] LessonsViewModel: LoadLessons started");
        try
        {
            IsBusy = true;
            var lessons = await _lessonService.GetLessonsAsync(_authenticationService.CurrentUserId);
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] LessonsViewModel: Received {lessons?.Count() ?? 0} lessons from service");

            if (lessons == null)
            {
                _allLessons = Enumerable.Empty<LessonResponse>();
            }
            else
            {
                var endOfWeek = _startOfWeek.AddDays(7);
                _allLessons = lessons
                    .Where(l => l.StartTime.Date >= _startOfWeek.Date && l.StartTime.Date < endOfWeek.Date)
                    .OrderBy(l => l.StartTime)
                    .ToList();
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] LessonsViewModel: Filtered for week: {_allLessons.Count()} lessons");
            FilterLessonsForSelectedDay();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] LessonsViewModel: Error in LoadLessons: {ex}");
        }
        finally
        {
            IsBusy = false;
            System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] LessonsViewModel: LoadLessons finished");
        }
    }

    // Navigates to LessonDetailPage, passing the full lesson object via the query dictionary
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
            await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
            {
                { "Lesson", lesson }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LessonsViewModel: Navigation error: {ex.Message}");
        }
    }

    // Navigates to the full weekly overview page
    [RelayCommand]
    private async Task GoToWeekView()
    {
        await Shell.Current.GoToAsync("//WeekPage");
    }

    // Handles CollectionView SelectionChanged event — navigates on selection and clears it
    // so the same item can be tapped again
    [RelayCommand]
    private async Task SelectionChanged(LessonResponse? lesson)
    {
        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: SelectionChanged called with lesson: {lesson?.WorkoutName ?? "null"}");

        if (lesson != null)
        {
            await GoToDetails(lesson);
            SelectedLesson = null;  // reset so the user can tap the same row again
        }
    }

    // Additional handler on SelectedLesson property change — ensures navigation runs
    // even when the CollectionView fires this instead of SelectionChanged
    partial void OnSelectedLessonChanged(LessonResponse? value)
    {
        System.Diagnostics.Debug.WriteLine($"LessonsViewModel: SelectedLesson changed to: {value?.WorkoutName ?? "null"}");

        if (value != null)
        {
            Task.Run(async () =>
            {
                await GoToDetails(value);
                MainThread.BeginInvokeOnMainThread(() => SelectedLesson = null);
            });
        }
    }
}
