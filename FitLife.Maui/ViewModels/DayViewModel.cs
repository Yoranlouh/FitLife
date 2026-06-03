using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Day detail page — shows all lessons on a single selected day.
// Implements IQueryAttributable so Shell can pass the target date via navigation query string.
public partial class DayViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ILessonService _lessonService;

    // The date whose lessons are displayed — set by Shell navigation query
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    // All lessons for the selected day, shown in the CollectionView
    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    public DayViewModel(ILessonService lessonService)
    {
        Title          = "Dagoverzicht";
        _lessonService = lessonService;
    }

    // Called by Shell when navigating with a query string, e.g. "DayPage?date=2024-06-10".
    // Parses the date string and triggers a lesson load for that day.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("date", out var dateObj)
            && dateObj is string dateStr
            && DateTime.TryParse(dateStr, out var date))
        {
            SelectedDate = date;
            _ = LoadLessons();  // fire-and-forget — the UI shows a spinner via IsBusy
        }
    }

    // Auto-triggered by the CommunityToolkit source generator whenever SelectedDate changes
    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadLessons();
    }

    // Fetches all lessons from the API and filters them to the selected date.
    // Uses IsBusy guard to prevent overlapping requests.
    [RelayCommand]
    private async Task LoadLessons()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var allLessons = await _lessonService.GetLessonsAsync();

            // Filter server result to only the chosen day and sort by start time
            var filteredLessons = allLessons
                .Where(l => l.StartTime.Date == SelectedDate.Date)
                .OrderBy(l => l.StartTime)
                .ToList();

            Lessons.Clear();
            foreach (var lesson in filteredLessons)
                Lessons.Add(lesson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading lessons for day: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Navigates to the lesson detail page, passing the full LessonResponse object
    // via the Shell navigation query dictionary.
    [RelayCommand]
    private async Task GoToDetails(LessonResponse lesson)
    {
        await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }
}
