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

    private void FilterLessonsForSelectedDay()
    {
        Lessons.Clear();
        foreach (var lesson in _allLessons
            .Where(l => l.StartTime.Date == SelectedDate.Date)
            .OrderBy(l => l.StartTime))
        {
            Lessons.Add(lesson);
        }
    }

    [RelayCommand]
    private void SelectDay(DateTime date)
    {
        SelectedDate = date;
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

    [RelayCommand]
    private async Task GoToDetails(LessonResponse lesson)
    {
        if (lesson == null) return;

        await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }

    [RelayCommand]
    private async Task GoToWeekView()
    {
        await Shell.Current.GoToAsync("//WeekPage");
    }
}
