using FitLife.Maui.ViewModels;
using SharedLibrary.DTOs.Responses;
using System.Collections.Specialized;

namespace FitLife.Maui.Views;

/// <summary>
/// Lessons list page - shows lessons filtered by selected day from the week
/// </summary>
public partial class LessonsPage : ContentPage
{
    private LessonsViewModel? _viewModel;
    private bool _isInitialized = false;

	public LessonsPage(LessonsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        _viewModel = viewModel;

        System.Diagnostics.Debug.WriteLine("LessonsPage: Initialized");

        // Subscribe to collection changes to add tap gestures for day headers only
        _viewModel.WeekDays.CollectionChanged += OnWeekDaysCollectionChanged;
	}

    /// <summary>
    /// Handle lesson item tapped - same pattern as MultipleLessonsPopup
    /// </summary>
    private async void OnLessonTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("LessonsPage: OnLessonTapped called");
        
        if (e.Parameter is LessonResponse lesson)
        {
            System.Diagnostics.Debug.WriteLine($"LessonsPage: Lesson tapped - {lesson.WorkoutName}");
            
            if (_viewModel != null)
            {
                await _viewModel.GoToDetailsCommand.ExecuteAsync(lesson);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"LessonsPage: Tapped parameter is not LessonResponse, it's: {e.Parameter?.GetType().Name ?? "null"}");
        }
    }

    /// <summary>
    /// Add tap gestures to day headers when collection changes
    /// </summary>
    private void OnWeekDaysCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"LessonsPage: WeekDays collection changed. Action: {e.Action}");
        
        // Wait a bit for the UI to render
        Dispatcher.Dispatch(() =>
        {
            AttachDayHeaderGestures();
        });
    }



    /// <summary>
    /// Attach tap gestures to day header borders
    /// </summary>
    private void AttachDayHeaderGestures()
    {
        if (_viewModel == null) return;

        System.Diagnostics.Debug.WriteLine($"LessonsPage: Attaching day header gestures. WeekDays count: {_viewModel.WeekDays.Count}");

        // Find all Border elements in the FlexLayout
        var borders = DayHeadersContainer.Children.OfType<Border>().ToList();
        System.Diagnostics.Debug.WriteLine($"LessonsPage: Found {borders.Count} day header borders");

        for (int i = 0; i < borders.Count && i < _viewModel.WeekDays.Count; i++)
        {
            var border = borders[i];
            var dayHeader = _viewModel.WeekDays[i];

            // Remove existing gesture recognizers
            border.GestureRecognizers.Clear();

            // Add new tap gesture
            var tapGesture = new TapGestureRecognizer();
            var dateToSelect = dayHeader.Date; // Capture the date in closure
            tapGesture.Tapped += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"LessonsPage: Day header tapped. Date: {dateToSelect:yyyy-MM-dd}");
                _viewModel?.SelectDayCommand.Execute(dateToSelect);
            };
            border.GestureRecognizers.Add(tapGesture);
        }

        System.Diagnostics.Debug.WriteLine("LessonsPage: Day header gestures attached");
    }



    /// <summary>
    /// Load lessons when page appears
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine("LessonsPage: OnAppearing called");

        if (_viewModel != null)
        {
            System.Diagnostics.Debug.WriteLine($"LessonsPage: Loading lessons. WeekDays count: {_viewModel.WeekDays.Count}, SelectedDate: {_viewModel.SelectedDate:yyyy-MM-dd}");
            await _viewModel.LoadLessonsCommand.ExecuteAsync(null);
            System.Diagnostics.Debug.WriteLine($"LessonsPage: Lessons loaded. Count: {_viewModel.Lessons.Count}");

            // Attach gestures to day headers after initial load
            if (!_isInitialized)
            {
                _isInitialized = true;
                AttachDayHeaderGestures();
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.WeekDays.CollectionChanged -= OnWeekDaysCollectionChanged;
        }
    }

    private async void OnBackToHomeClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//HomePage");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}