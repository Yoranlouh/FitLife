using FitLife.Maui.Helpers;
using FitLife.Maui.ViewModels;
using SharedLibrary.DTOs.Responses;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;

namespace FitLife.Maui.Views;

public partial class WeekPage
{
    private const int StartHour = 8;  // 08:00
    private const int EndHour = 20;   // 20:00
    private const int TotalRows = EndHour - StartHour; // 12 rijen

    private bool _isUpdating;
    private bool _isGridInitialized;
    private WeekViewModel? _viewModel;

	public WeekPage(WeekViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        _viewModel = viewModel;
        _viewModel.Lessons.CollectionChanged += OnLessonsCollectionChanged;
	}

    // Debounce-token voor grid-herbouw. Eén lessen-load veroorzaakt 1 Clear + N Adds.
    // Die changes komen NIET als één atomair main-thread-blok binnen (bewezen via logcat:
    // tussen elke Add draait de herbouw al), dus een simpele "pending"-vlag coalesceert niet
    // en elke Add triggert een volledige UpdateLessonGrid → de load wordt O(N²) en de
    // UI-thread bevriest (ANR). Elke change verhoogt de generatie en plant een uitgestelde
    // herbouw; alleen de laatst geplande herbouw van een burst draait, dus de grid wordt
    // precies één keer per load opgebouwd — dezelfde granulariteit als een CollectionView.
    private int _gridUpdateGeneration;

    private void OnLessonsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdating || !_isGridInitialized) return;

        // DispatchDelayed is veilig vanaf elke thread; de actie draait op de main thread.
        var generation = ++_gridUpdateGeneration;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            // Een nieuwere change in dezelfde burst heeft deze herbouw verdrongen.
            if (generation != _gridUpdateGeneration) return;
            if (!_isUpdating && _isGridInitialized)
            {
                System.Diagnostics.Debug.WriteLine($"[WeekPage] Grid bijwerken — {(_viewModel?.Lessons.Count ?? 0)} lessen");
                UpdateLessonGrid();
            }
        });
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            _viewModel.Lessons.CollectionChanged -= OnLessonsCollectionChanged;
        }
        // Annuleer een eventueel uitgestelde (gedebouncede) herbouw zodat die niet
        // alsnog afvuurt nadat de pagina is verdwenen.
        _gridUpdateGeneration++;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Re-register handler zodat grid-updates werken na terugnavigeren.
        if (_viewModel != null)
        {
            _viewModel.Lessons.CollectionChanged -= OnLessonsCollectionChanged;
            _viewModel.Lessons.CollectionChanged += OnLessonsCollectionChanged;
        }

        // Dispatcher.Dispatch stelt grid-init en data-load uit naar NADAT de
        // navigatieanimatie klaar is. Synchroon werk in OnAppearing zelf blokkeert
        // de animatie en veroorzaakt een zichtbare freeze op Android/Windows.
        Dispatcher.Dispatch(() =>
        {
            if (!_isGridInitialized)
            {
                InitializeGrid();
                _isGridInitialized = true;
            }

            if (BindingContext is WeekViewModel viewModel)
                _ = viewModel.LoadLessonsCommand.ExecuteAsync(null);
        });
    }

    private void InitializeGrid()
    {
        if (LessonGrid == null) return;

        LessonGrid.BatchBegin();
        try
        {
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeGrid ERROR: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            LessonGrid.BatchCommit();
        }
    }

    private void UpdateLessonGrid()
    {
        if (_isUpdating) return;
        if (LessonGrid == null) return;
        if (BindingContext is not WeekViewModel viewModel) return;

        System.Diagnostics.Debug.WriteLine($"[WeekPage] UpdateLessonGrid start — {viewModel.Lessons.Count} lessen");
        LessonGrid.BatchBegin();
        try
        {
            _isUpdating = true;

            // Verwijder alleen de bestaande lessen
            var toRemove = LessonGrid.Children.Where(c => c is Border b && b.StyleId == "LessonBlock").ToList();
            foreach (var child in toRemove)
            {
                LessonGrid.Children.Remove(child);
            }

            if (viewModel.Lessons.Count == 0)
            {
                return;
            }

            var diff = (7 + ((int)viewModel.CurrentDate.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
            var startOfWeek = viewModel.CurrentDate.Date.AddDays(-diff);
            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Start of week: {startOfWeek:yyyy-MM-dd}");

            // Groepeer lessen per dag en uur om overlapping te detecteren
            var lessonsBySlot = viewModel.Lessons
                .GroupBy(l => new { Day = (l.StartTime.Date - startOfWeek).Days, l.StartTime.Hour })
                .ToDictionary(g => g.Key, g => g.ToList());

            System.Diagnostics.Debug.WriteLine($"UpdateLessonGrid - Grouped into {lessonsBySlot.Count} slots");

            foreach (var slot in lessonsBySlot)
            {
                int col = slot.Key.Day + 1;
                int row = slot.Key.Hour - StartHour;

                if (col < 1 || col > 7 || row < 0 || row >= TotalRows)
                {
                    continue;
                }

                var lessonsInSlot = slot.Value;
                int count = lessonsInSlot.Count;

                // Bereken totaal aantal beschikbare plekken
                int totalAvailableSpots = lessonsInSlot.Sum(l => Math.Max(0, l.MaxParticipants - l.CurrentParticipantCount));
                bool isAnyBooked = lessonsInSlot.Any(l => l.IsBooked);

                var border = new Border
                {
                    StyleId = "LessonBlock",
                    StrokeThickness = 0,
                    Margin = new Thickness(1),
                    BackgroundColor = Colors.Transparent // Achtergrond wordt gevuld door shapes of Grid
                };

                var cellGrid = new Grid();
                border.Content = cellGrid;

                // Kleuren instellen
                if (count == 1)
                {
                    border.BackgroundColor = GetWorkoutColor(lessonsInSlot.First().WorkoutColor);
                }
                else
                {
                    var colors = lessonsInSlot.Select(l => GetWorkoutColor(l.WorkoutColor)).ToList();
                    AddDiagonalBackground(cellGrid, colors);
                }

                // Cirkel met nummer of vinkje
                var circleSize = 30;
                var circleContainer = new Grid
                {
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    WidthRequest = circleSize,
                    HeightRequest = circleSize
                };

                var circle = new Border
                {
                    BackgroundColor = Colors.White,
                    StrokeShape = new RoundRectangle { CornerRadius = circleSize / 2 },
                    WidthRequest = circleSize,
                    HeightRequest = circleSize,
                    Padding = 0,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                if (isAnyBooked)
                {
                    // Toon vinkje: gebruiker is ingeschreven voor minstens één les in dit slot
                    circle.Content = new Label
                    {
                        Text = "✓",
                        TextColor = Colors.Black,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                }
                else
                {
                    // Toon totaal aantal plekken
                    circle.Content = new Label
                    {
                        Text = totalAvailableSpots.ToString(),
                        TextColor = Colors.Black,
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                }

                cellGrid.Children.Add(circleContainer);
                circleContainer.Children.Add(circle);

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (_, _) =>
                {
                    try
                    {
                        if (count == 1)
                        {
                            await viewModel.GoToDetailsCommand.ExecuteAsync(lessonsInSlot.First());
                        }
                        else
                        {
                            var popup = new MultipleLessonsPopup(lessonsInSlot);
                            var popupResult = await Navigation.ShowPopupAsync<LessonResponse>(popup, PopupOptions.Empty, CancellationToken.None);

                            if (!popupResult.WasDismissedByTappingOutsideOfPopup && popupResult.Result is { } selectedLesson)
                            {
                                await viewModel.GoToDetailsCommand.ExecuteAsync(selectedLesson);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WeekPage] Tap gesture error: {ex.Message}\n{ex.StackTrace}");
                    }
                };
                border.GestureRecognizers.Add(tapGesture);

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                LessonGrid.Children.Add(border);
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeekPage] UpdateLessonGrid fout: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            LessonGrid.BatchCommit();
            _isUpdating = false;
            System.Diagnostics.Debug.WriteLine("[WeekPage] UpdateLessonGrid klaar");
        }
    }

    // Splits a slot cell into proportional colour stripes for overlapping lessons.
    // Uses BoxViews in a star-sized Grid instead of Polygon shapes: MAUI Shape elements
    // with Aspect=Stretch.Fill and no fixed size enter a measure/arrange loop when placed
    // in a star-width cell inside a ScrollView, which freezes the UI thread and prevents
    // the grid from ever rendering. BoxViews have a determinate size and lay out cheaply.
    private void AddDiagonalBackground(Grid grid, List<Color> colors)
    {
        if (colors.Count < 2) return;

        var stripeGrid = new Grid { ColumnSpacing = 0 };
        for (int i = 0; i < colors.Count; i++)
            stripeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        for (int i = 0; i < colors.Count; i++)
        {
            var box = new BoxView { Color = colors[i] };
            Grid.SetColumn(box, i);
            stripeGrid.Children.Add(box);
        }

        grid.Children.Add(stripeGrid);
    }

    // Delegates to the central WorkoutColorHelper so all views use one source for color resolution.
    private static Color GetWorkoutColor(string? workoutColor)
        => WorkoutColorHelper.Resolve(workoutColor);

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//HomePage");
        return true;
    }
}