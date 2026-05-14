using System.Collections.ObjectModel;
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

    public LessonsViewModel(ILessonService lessonService)
    {
        Title = "Lesaanbod";
        _lessonService = lessonService;
    }

    [RelayCommand]
    public async Task LoadLessons()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var lessons = await _lessonService.GetLessonsAsync();
            
            Lessons.Clear();
            foreach (var lesson in lessons)
            {
                Lessons.Add(lesson);
            }
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
}
