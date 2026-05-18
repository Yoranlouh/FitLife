using FitLife.Maui.ViewModels;
using SharedLibrary.DTOs.Responses;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui.Views; 

namespace FitLife.Maui.Views;

public partial class WeekPage : ContentPage
{
    private const int StartHour = 8;  // 08:00
    private const int EndHour = 20;   // 20:00
    private const int TotalRows = EndHour - StartHour; // 12 rijen

    private bool _isUpdating = false;
    private bool _isGridInitialized = false;
    private WeekViewModel? _viewModel;

	public WeekPage(WeekViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        _viewModel = viewModel;
        _viewModel.Lessons.CollectionChanged += OnLessonsCollectionChanged;
	}

    private CancellationTokenSource? _updateCts;

    private void OnLessonsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdating || !_isGridInitialized) return;

        // Annuleer de vorige geplande update
        _updateCts?.Cancel();
        _updateCts = new CancellationTokenSource();
        var token = _updateCts.Token;

        // Wacht even om te zien of er meer updates komen (debouncing)
        Task.Delay(50, token).ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully && !token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_isUpdating && _isGridInitialized)
                    {
                        UpdateLessonGrid();
                    }
                });
            }
        }, TaskScheduler.Default);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            _viewModel.Lessons.CollectionChanged -= OnLessonsCollectionChanged;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_isGridInitialized)
        {
            // Initialiseer grid asynchroon om UI freeze te voorkomen
            Dispatcher.Dispatch(() =>
            {
                InitializeGrid();
                _isGridInitialized = true;

                if (BindingContext is WeekViewModel viewModel)
                {
                    viewModel.LoadLessonsCommand.Execute(null);
                }
            });
        }
        else
        {
            if (BindingContext is WeekViewModel viewModel)
            {
                viewModel.LoadLessonsCommand.Execute(null);
            }
        }
    }

    private void InitializeGrid()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("InitializeGrid - Starting");

            // Voeg alleen tijdlabels toe (veel sneller, geen 84 borders!)
            for (int i = 0; i < TotalRows; i++)
            {
                var label = new Label
                {
                    Text = $"{StartHour + i}:00",
                    TextColor = Colors.Black,
                    FontSize = 11,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, i);
                Grid.SetColumn(label, 0);
                LessonGrid.Children.Add(label);
            }

            System.Diagnostics.Debug.WriteLine($"InitializeGrid - Completed. Total children: {LessonGrid.Children.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeGrid ERROR: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void UpdateLessonGrid()
    {
        if (_isUpdating) return;
        if (BindingContext is not WeekViewModel viewModel) return;

        try
        {
            _isUpdating = true;
            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Starting. Lessons count: {viewModel.Lessons.Count}");

            // Verwijder alleen de bestaande lessen
            var toRemove = LessonGrid.Children.Where(c => c is Border b && b.StyleId == "LessonBlock").ToList();
            foreach (var child in toRemove)
            {
                LessonGrid.Children.Remove(child);
            }
            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Removed {toRemove.Count} old lesson blocks");

            if (viewModel.Lessons.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("UpdateLessonGrid - No lessons to display");
                return;
            }

            var diff = (7 + ((int)viewModel.CurrentDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
            var startOfWeek = viewModel.CurrentDate.Date.AddDays(-diff);
            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Start of week: {startOfWeek:yyyy-MM-dd}");

            // Groepeer lessen per dag en uur om overlapping te detecteren
            var lessonsBySlot = viewModel.Lessons
                .GroupBy(l => new { Day = (l.StartTime.Date - startOfWeek).Days, Hour = l.StartTime.Hour })
                .ToDictionary(g => g.Key, g => g.ToList());

            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Grouped into {lessonsBySlot.Count} slots");

            foreach (var slot in lessonsBySlot)
            {
                int col = slot.Key.Day + 1;
                int row = slot.Key.Hour - StartHour;

                System.Diagnostics.Debug.WriteLine($"Slot: Day={slot.Key.Day}, Hour={slot.Key.Hour}, Col={col}, Row={row}");

                if (col < 1 || col > 7 || row < 0 || row >= TotalRows)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping slot - out of bounds: Day={slot.Key.Day}, Hour={slot.Key.Hour}, Col={col}, Row={row}");
                    continue;
                }

                var lessonsInSlot = slot.Value;
                int count = lessonsInSlot.Count;

                // Toon de naam van de les als er maar één is, anders het aantal
                string displayText = count == 1 ? (lessonsInSlot.First().WorkoutName ?? "Les") : count.ToString();
                var workoutName = lessonsInSlot.First()?.WorkoutName ?? "Unknown";
                var color = GetWorkoutColor(workoutName);

                var border = new Border
                {
                    StyleId = "LessonBlock",
                    BackgroundColor = color,
                    StrokeThickness = 0,
                    Margin = new Thickness(1),
                    Content = new Label
                    {
                        Text = displayText,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = count == 1 ? 10 : 14,
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.TailTruncation,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    if (count == 1)
                    {
                        // Navigeer direct naar de les details
                        await viewModel.GoToDetailsCommand.ExecuteAsync(lessonsInSlot.First());
                    }
                    else
                    {
                        // Toon pop-up met meerdere lessen
                        var popup = new MultipleLessonsPopup(lessonsInSlot);
                        var result = await Shell.Current.CurrentPage.ShowPopupAsync(popup);

                        if (result is LessonResponse selectedLesson)
                        {
                            await viewModel.GoToDetailsCommand.ExecuteAsync(selectedLesson);
                        }
                    }
                };
                border.GestureRecognizers.Add(tapGesture);

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                LessonGrid.Children.Add(border);
                System.Diagnostics.Debug.WriteLine($"Added lesson block at Row={row}, Col={col}");
            }

            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Completed. Total children now: {LessonGrid.Children.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in UpdateLessonGrid: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private static Color GetWorkoutColor(string? workoutName)
    {
        if (string.IsNullOrEmpty(workoutName))
            return GetResourceColor("Primary");

        // Geef verschillende kleuren op basis van workout type
        return workoutName.ToLower() switch
        {
            var n when n.Contains("crossfit") => GetResourceColor("WorkoutCrossfit"),
            var n when n.Contains("spinning") => GetResourceColor("WorkoutSpinning"),
            var n when n.Contains("sweat club") => GetResourceColor("WorkoutSweatClub"),
            var n when n.Contains("open gym") => GetResourceColor("WorkoutOpenGym"),
            var n when n.Contains("hyrox") => GetResourceColor("WorkoutHyrox"),
            var n when n.Contains("gymnastics") => GetResourceColor("WorkoutGymnastics"),
            _ => GetResourceColor("Primary")
        };
    }

    private static Color GetResourceColor(string key)
    {
        if (Application.Current!.Resources.TryGetValue(key, out var color))
            return (Color)color;
        
        return Colors.Gray;
    }
}
