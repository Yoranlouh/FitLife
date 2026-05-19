using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using System.Globalization;
using CommunityToolkit.Maui.Views;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

public partial class WeekViewModel : BaseViewModel
{
    private readonly ILessonService _lessonService;

    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    [ObservableProperty]
    private DateTime _currentDate = DateTime.Today;

    [ObservableProperty]
    private string _weekRangeText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DayHeaderViewModel> _weekDays = new();

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<LessonResponse> _selectedDayLessons = new();

    private DateTime _startOfWeek;
    private IEnumerable<LessonResponse> _allLessons = [];

    public WeekViewModel(ILessonService lessonService)
    {
        _lessonService = lessonService;
        Title = "Weekoverzicht";
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
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateWeekDaySelection();
        FilterLessonsForSelectedDay();
    }

    private void UpdateWeekDaySelection()
    {
        // Voorkom oneindige loops door alleen te updaten als de waarde verandert
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
        SelectedDayLessons.Clear();
        foreach (var lesson in Lessons.Where(l => l.StartTime.Date == SelectedDate.Date)
                                      .OrderBy(l => l.StartTime))
        {
            SelectedDayLessons.Add(lesson);
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
        _ = LoadLessons();
    }

    [RelayCommand]
    private void NextWeek()
    {
        CurrentDate = CurrentDate.AddDays(7);
        UpdateWeekInfo();
        _ = LoadLessons();
    }

    [RelayCommand]
    private async Task LoadLessons()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var lessons = await _lessonService.GetLessonsAsync();
            _allLessons = lessons.ToList();

            // Filter voor de huidige week
            var endOfWeek = _startOfWeek.AddDays(7);
            var filteredLessons = _allLessons
                .Where(l => l.StartTime >= _startOfWeek && l.StartTime < endOfWeek)
                .OrderBy(l => l.StartTime)
                .ToList();

            Lessons.Clear();
            foreach (var lesson in filteredLessons)
            {
                Lessons.Add(lesson);
            }

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

    [RelayCommand]
    private async Task GoToDayView(DateTime date)
    {
        await Shell.Current.GoToAsync($"DayPage?date={date:yyyy-MM-dd}");
    }

    [RelayCommand]
    public async Task GoToDetails(LessonResponse lesson)
    {
        await Shell.Current.GoToAsync($"LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }

    [RelayCommand]
    private async Task ShowLegend()
    {
        var popup = new Views.LegendPopup();
        await Shell.Current.CurrentPage.ShowPopupAsync(popup);
    }

    // Note: GoToLessonsCommand is inherited from BaseViewModel
}

public partial class DayHeaderViewModel : ObservableObject
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    
    [ObservableProperty]
    private bool _isSelected;
}
