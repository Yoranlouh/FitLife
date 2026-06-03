using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitLife.Maui.Services;

namespace FitLife.Maui.ViewModels;

// ViewModel for the Create/Edit lesson form used by instructors and admins.
// QueryProperty allows Shell to pass a LessonId when navigating for editing;
// a value of 0 means "create new lesson".
[QueryProperty(nameof(LessonId), "LessonId")]
public partial class ManageLessonViewModel : BaseViewModel
{
    private readonly ILessonManagementService _lessonManagementService;
    private readonly IAuthenticationService   _authService;

    // The lesson being edited (0 = create mode)
    [ObservableProperty] private int      _lessonId;
    [ObservableProperty] private bool     _isEditMode;

    // Form fields for the lesson date/time
    [ObservableProperty] private DateTime _startDate      = DateTime.Today;
    [ObservableProperty] private TimeSpan _startTime      = new(9, 0, 0);
    [ObservableProperty] private TimeSpan _endTime        = new(10, 0, 0);

    [ObservableProperty] private int      _maxParticipants = 15;
    [ObservableProperty] private string   _statusMessage   = string.Empty;

    // True when the logged-in user is an admin (can choose any instructor);
    // false for instructors (auto-assigned to themselves)
    [ObservableProperty] private bool     _isAdmin;
    [ObservableProperty] private string   _addMemberInput  = string.Empty;
    [ObservableProperty] private string   _saveButtonText  = "Les Aanmaken";

    // True after dropdown data is loaded; hides the loading overlay
    [ObservableProperty] private bool     _dataLoaded;

    // Currently selected items — NotifyPropertyChangedFor also refreshes the display-name computed properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedWorkoutName))]
    private SimpleItemDto? _selectedWorkout;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedLocationName))]
    private SimpleItemDto? _selectedLocation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedInstructorName))]
    private SimpleItemDto? _selectedInstructor;

    // Display-only strings shown in the picker buttons
    public string SelectedWorkoutName    => SelectedWorkout?.Name    ?? "Selecteer workout...";
    public string SelectedLocationName   => SelectedLocation?.Name   ?? "Selecteer locatie...";
    public string SelectedInstructorName => SelectedInstructor?.Name ?? "Selecteer instructeur...";

    // Dropdown options loaded from the API at page load
    public ObservableCollection<SimpleItemDto> Workouts    { get; } = new();
    public ObservableCollection<SimpleItemDto> Locations   { get; } = new();
    public ObservableCollection<SimpleItemDto> Instructors { get; } = new();

    public ManageLessonViewModel(ILessonManagementService lessonManagementService,
                                  IAuthenticationService  authService)
    {
        _lessonManagementService = lessonManagementService;
        _authService             = authService;
        IsAdmin                  = authService.IsAdmin;
        Title                    = "Les Aanmaken";
    }

    // Called automatically when LessonId is set via Shell query.
    // Switches the form between create mode (id=0) and edit mode (id>0).
    partial void OnLessonIdChanged(int value)
    {
        IsEditMode     = value > 0;
        Title          = IsEditMode ? "Les Wijzigen" : "Les Aanmaken";
        SaveButtonText = IsEditMode ? "Les Opslaan"  : "Les Aanmaken";
    }

    // Fetches workouts, locations, and instructors from the API in parallel.
    // If the trainer is not an admin, they are automatically set as the instructor.
    // Guards with IsBusy to prevent duplicate calls on repeated OnAppearing events.
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusMessage = string.Empty;
        DataLoaded    = false;

        try
        {
            // Start all three API calls at the same time
            var workoutsTask    = _lessonManagementService.GetWorkoutsAsync();
            var locationsTask   = _lessonManagementService.GetLocationsAsync();
            var instructorsTask = _lessonManagementService.GetInstructorsAsync();

            // Wait until all three finish before populating the collections
            await Task.WhenAll(workoutsTask, locationsTask, instructorsTask);

            var workouts    = workoutsTask.Result.ToList();
            var locations   = locationsTask.Result.ToList();
            var instructors = instructorsTask.Result.ToList();

            Workouts.Clear();
            foreach (var w in workouts)    Workouts.Add(w);

            Locations.Clear();
            foreach (var l in locations)   Locations.Add(l);

            Instructors.Clear();
            foreach (var i in instructors) Instructors.Add(i);

            // Trainers (non-admins) are auto-assigned as the instructor for the lesson
            if (!IsAdmin && _authService.CurrentUserId.HasValue)
            {
                SelectedInstructor = Instructors.FirstOrDefault(i => i.Id == _authService.CurrentUserId.Value)
                                     ?? Instructors.FirstOrDefault();
            }

            if (Workouts.Count == 0 && Locations.Count == 0)
                StatusMessage = "⚠️ Kon gegevens niet laden. Controleer de API-verbinding.";
            else
                DataLoaded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠️ Fout bij laden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Shows an ActionSheet (iOS-style bottom sheet) listing all available workouts.
    // Disabled with a status message if the data hasn't loaded yet.
    [RelayCommand]
    private async Task SelectWorkout()
    {
        if (!Workouts.Any()) { StatusMessage = "Workouts worden geladen..."; return; }

        var names  = Workouts.Select(w => w.Name).ToArray();
        var chosen = await Shell.Current.DisplayActionSheet("Kies een workout", "Annuleren", null, names);
        if (!string.IsNullOrEmpty(chosen) && chosen != "Annuleren")
            SelectedWorkout = Workouts.FirstOrDefault(w => w.Name == chosen);
    }

    // Shows an ActionSheet listing all available locations/halls.
    [RelayCommand]
    private async Task SelectLocation()
    {
        if (!Locations.Any()) { StatusMessage = "Locaties worden geladen..."; return; }

        var names  = Locations.Select(l => l.Name).ToArray();
        var chosen = await Shell.Current.DisplayActionSheet("Kies een locatie", "Annuleren", null, names);
        if (!string.IsNullOrEmpty(chosen) && chosen != "Annuleren")
            SelectedLocation = Locations.FirstOrDefault(l => l.Name == chosen);
    }

    // Shows an ActionSheet listing all instructors (admin-only — trainers see a read-only field).
    [RelayCommand]
    private async Task SelectInstructor()
    {
        if (!Instructors.Any()) { StatusMessage = "Instructeurs worden geladen..."; return; }

        var names  = Instructors.Select(i => i.Name).ToArray();
        var chosen = await Shell.Current.DisplayActionSheet("Kies een instructeur", "Annuleren", null, names);
        if (!string.IsNullOrEmpty(chosen) && chosen != "Annuleren")
            SelectedInstructor = Instructors.FirstOrDefault(i => i.Name == chosen);
    }

    // Validates the form and either creates or updates the lesson via the API.
    // Navigates back on success, or shows a status message on failure.
    [RelayCommand]
    private async Task SaveLesson()
    {
        // Validate required fields before calling the API
        if (SelectedWorkout is null)    { StatusMessage = "Selecteer een workout type."; return; }
        if (SelectedLocation is null)   { StatusMessage = "Selecteer een locatie."; return; }
        if (SelectedInstructor is null) { StatusMessage = "Selecteer een instructeur."; return; }
        if (EndTime <= StartTime)       { StatusMessage = "Eindtijd moet na de begintijd liggen."; return; }

        // Build the request DTO from form fields
        var request = new LessonSaveRequest
        {
            StartTime       = StartDate.Date + StartTime,
            EndTime         = StartDate.Date + EndTime,
            MaxParticipants = MaxParticipants,
            WorkoutId       = SelectedWorkout.Id,
            InstructorId    = SelectedInstructor.Id,
            LocationId      = SelectedLocation.Id
        };

        IsBusy        = true;
        StatusMessage = string.Empty;
        try
        {
            // Route to update or create depending on edit mode
            var (success, message) = IsEditMode
                ? await _lessonManagementService.UpdateLessonAsync(LessonId, request)
                : await _lessonManagementService.CreateLessonAsync(request);

            if (success)
            {
                await Shell.Current.DisplayAlert(IsEditMode ? "Bijgewerkt" : "Les aangemaakt", message, "OK");
                await Shell.Current.GoToAsync("..");  // navigate back to the lesson list
            }
            else
            {
                StatusMessage = message;  // display server error below the form
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Admin-only: manually adds a member (by user ID) to a lesson without deducting credits.
    // Only available in edit mode — requires the lesson to already exist in the database.
    [RelayCommand]
    private async Task AddMember()
    {
        if (!IsEditMode || LessonId <= 0)
        {
            StatusMessage = "Sla de les eerst op voordat je leden toevoegt.";
            return;
        }
        if (!int.TryParse(AddMemberInput.Trim(), out int userId))
        {
            StatusMessage = "Voer een geldig gebruikers-ID in.";
            return;
        }

        IsBusy = true;
        try
        {
            var (success, message) = await _lessonManagementService.AddMemberToLessonAsync(LessonId, userId);
            StatusMessage = message;
            if (success) AddMemberInput = string.Empty;  // clear the input field on success
        }
        finally
        {
            IsBusy = false;
        }
    }
}
