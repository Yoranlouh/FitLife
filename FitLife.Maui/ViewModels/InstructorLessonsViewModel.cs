using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

public partial class InstructorLessonsViewModel : BaseViewModel
{
    private readonly ILessonManagementService _lessonManagementService;
    private readonly IAuthenticationService _authService;

    public ObservableCollection<LessonResponse> Lessons { get; } = new();

    [ObservableProperty]
    private string _emptyMessage = "Je hebt geen geplande lessen als trainer.";

    public InstructorLessonsViewModel(ILessonManagementService lessonManagementService,
                                      IAuthenticationService authService)
    {
        _lessonManagementService = lessonManagementService;
        _authService = authService;
        Title = "Mijn Lessen als Trainer";
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        var instructorId = _authService.CurrentUserId;
        if (instructorId is null or <= 0) return;

        try
        {
            IsBusy = true;
            var lessons = await _lessonManagementService.GetInstructorLessonsAsync(instructorId.Value);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Lessons.Clear();
                foreach (var l in lessons.OrderBy(x => x.StartTime))
                    Lessons.Add(l);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenManageLesson(LessonResponse lesson)
    {
        var query = new Dictionary<string, object> { { "LessonId", lesson.Id } };
        await Shell.Current.GoToAsync("ManageLessonPage", query);
    }

    [RelayCommand]
    private async Task AddNewLesson()
    {
        await Shell.Current.GoToAsync("ManageLessonPage");
    }

    [RelayCommand]
    private async Task DeleteLesson(LessonResponse lesson)
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Verwijderen",
            $"Weet je zeker dat je '{lesson.WorkoutName}' op {lesson.StartTime:dd-MM-yyyy HH:mm} wilt verwijderen?",
            "Ja, verwijderen",
            "Annuleren");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var (success, message) = await _lessonManagementService.DeleteLessonAsync(lesson.Id);
            if (success)
            {
                Lessons.Remove(lesson);
                await Shell.Current.DisplayAlert("Verwijderd", message, "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Fout", message, "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}