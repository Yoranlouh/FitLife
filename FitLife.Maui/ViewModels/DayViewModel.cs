using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

public partial class DayViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ILessonService _lessonService;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<LessonResponse> _lessons = new();

    public DayViewModel(ILessonService lessonService)
    {
        Title = "Dagoverzicht";
        _lessonService = lessonService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("date", out var dateObj) && dateObj is string dateStr && DateTime.TryParse(dateStr, out var date))
        {
            SelectedDate = date;
            _ = LoadLessons();
        }
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadLessons();
    }

    [RelayCommand]
    private async Task LoadLessons()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var allLessons = await _lessonService.GetLessonsAsync();
            var filteredLessons = allLessons
                .Where(l => l.StartTime.Date == SelectedDate.Date)
                .OrderBy(l => l.StartTime)
                .ToList();

            Lessons.Clear();
            foreach (var lesson in filteredLessons)
            {
                Lessons.Add(lesson);
            }
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

    [RelayCommand]
    private async Task GoToDetails(LessonResponse lesson)
    {
        await Shell.Current.GoToAsync("LessonDetailPage", new Dictionary<string, object>
        {
            { "Lesson", lesson }
        });
    }
}
