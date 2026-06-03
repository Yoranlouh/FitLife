using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Instructor's own lesson overview page.
// Loads all lessons where the logged-in instructor is the teacher,
// and provides commands to create, edit, or delete those lessons.
public partial class InstructorLessonsViewModel : BaseViewModel
{
    private readonly ILessonManagementService _lessonManagementService;
    private readonly IAuthenticationService   _authService;

    // All lessons taught by the current instructor — bound to the page's CollectionView
    public ObservableCollection<LessonResponse> Lessons { get; } = new();

    // Shown when the instructor has no upcoming lessons
    [ObservableProperty]
    private string _emptyMessage = "Je hebt geen geplande lessen als trainer.";

    public InstructorLessonsViewModel(ILessonManagementService lessonManagementService,
                                      IAuthenticationService   authService)
    {
        _lessonManagementService = lessonManagementService;
        _authService             = authService;
        Title = "Mijn Lessen als Trainer";
    }

    // Fetches lessons for the logged-in instructor from GET /lessons/instructor/{id}.
    // Guard: returns early if the user is not authenticated or a request is already running.
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        var instructorId = _authService.CurrentUserId;
        if (instructorId is null or <= 0) return;

        try
        {
            IsBusy = true;
            var lessons = await _lessonManagementService.GetInstructorLessonsAsync(instructorId.Value);

            // Ensure UI updates run on the main thread (ObservableCollection is not thread-safe)
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

    // Opens the ManageLessonPage in edit mode for the selected lesson.
    // Passes the lesson ID via Shell navigation query so the page pre-fills the form.
    [RelayCommand]
    private async Task OpenManageLesson(LessonResponse lesson)
    {
        var query = new Dictionary<string, object> { { "LessonId", lesson.Id } };
        await Shell.Current.GoToAsync("ManageLessonPage", query);
    }

    // Opens the ManageLessonPage in create mode (no LessonId parameter).
    [RelayCommand]
    private async Task AddNewLesson()
    {
        await Shell.Current.GoToAsync("ManageLessonPage");
    }

    // Shows a confirmation alert, then calls the API to delete the lesson.
    // Only removes the item from the local list after a successful API response.
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
                Lessons.Remove(lesson);  // update UI only after server confirmed deletion
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
