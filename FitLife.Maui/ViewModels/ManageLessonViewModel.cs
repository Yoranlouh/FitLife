using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

[QueryProperty(nameof(LessonId), "LessonId")]
public partial class ManageLessonViewModel : BaseViewModel
{
    private readonly ILessonManagementService _lessonManagementService;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private int _lessonId;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _startTime = new(9, 0, 0);

    [ObservableProperty]
    private TimeSpan _endTime = new(10, 0, 0);

    [ObservableProperty]
    private int _maxParticipants = 15;

    [ObservableProperty]
    private SimpleItemDto? _selectedWorkout;

    [ObservableProperty]
    private SimpleItemDto? _selectedLocation;

    [ObservableProperty]
    private SimpleItemDto? _selectedInstructor;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private string _addMemberInput = string.Empty;

    public ObservableCollection<SimpleItemDto> Workouts { get; } = new();
    public ObservableCollection<SimpleItemDto> Locations { get; } = new();
    public ObservableCollection<SimpleItemDto> Instructors { get; } = new();

    public ManageLessonViewModel(ILessonManagementService lessonManagementService,
                                  IAuthenticationService authService)
    {
        _lessonManagementService = lessonManagementService;
        _authService = authService;
        IsAdmin = authService.IsAdmin;
        Title = "Les Aanmaken";
    }

    [ObservableProperty]
    private string _saveButtonText = "Les Aanmaken";

    partial void OnLessonIdChanged(int value)
    {
        IsEditMode = value > 0;
        Title = IsEditMode ? "Les Wijzigen" : "Les Aanmaken";
        SaveButtonText = IsEditMode ? "Les Opslaan" : "Les Aanmaken";
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var workoutsTask = _lessonManagementService.GetWorkoutsAsync();
            var locationsTask = _lessonManagementService.GetLocationsAsync();
            var instructorsTask = _lessonManagementService.GetInstructorsAsync();
            await Task.WhenAll(workoutsTask, locationsTask, instructorsTask);

            var workouts = workoutsTask.Result.ToList();
            var locations = locationsTask.Result.ToList();
            var instructors = instructorsTask.Result.ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Workouts.Clear();
                foreach (var w in workouts) Workouts.Add(w);

                Locations.Clear();
                foreach (var l in locations) Locations.Add(l);

                Instructors.Clear();
                foreach (var i in instructors) Instructors.Add(i);

                if (!IsAdmin && _authService.CurrentUserId.HasValue)
                {
                    SelectedInstructor = Instructors.FirstOrDefault(i => i.Id == _authService.CurrentUserId.Value)
                                         ?? Instructors.FirstOrDefault();
                }
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveLesson()
    {
        if (SelectedWorkout is null || SelectedLocation is null || SelectedInstructor is null)
        {
            StatusMessage = "Vul alle velden in: workout, locatie en instructeur.";
            return;
        }

        if (EndTime <= StartTime)
        {
            StatusMessage = "Eindtijd moet na de begintijd liggen.";
            return;
        }

        var request = new LessonSaveRequest
        {
            StartTime = StartDate.Date + StartTime,
            EndTime = StartDate.Date + EndTime,
            MaxParticipants = MaxParticipants,
            WorkoutId = SelectedWorkout.Id,
            InstructorId = SelectedInstructor.Id,
            LocationId = SelectedLocation.Id
        };

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var (success, message) = IsEditMode
                ? await _lessonManagementService.UpdateLessonAsync(LessonId, request)
                : await _lessonManagementService.CreateLessonAsync(request);

            StatusMessage = message;
            if (success)
            {
                await Shell.Current.DisplayAlert(IsEditMode ? "Bijgewerkt" : "Aangemaakt", message, "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddMember()
    {
        if (!IsEditMode || LessonId <= 0)
        {
            StatusMessage = "Sla de les eerst op voordat je leden toevoegt.";
            return;
        }

        var input = AddMemberInput.Trim();
        if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int userId))
        {
            StatusMessage = "Voer een geldig gebruikers-ID in.";
            return;
        }

        IsBusy = true;
        try
        {
            var (success, message) = await _lessonManagementService.AddMemberToLessonAsync(LessonId, userId);
            StatusMessage = message;
            if (success) AddMemberInput = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }
}