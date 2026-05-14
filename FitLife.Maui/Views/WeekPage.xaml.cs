using FitLife.Maui.ViewModels;
using SharedLibrary.DTOs.Responses;
using System.Globalization;

namespace FitLife.Maui.Views;

public partial class WeekPage : ContentPage
{
	public WeekPage(WeekViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateLessonGrid();
    }

    private void UpdateLessonGrid()
    {
        if (BindingContext is not WeekViewModel viewModel) return;

        // Clear existing lessons from the grid (those that are not time labels)
        var toRemove = LessonGrid.Children.Where(c => c is Border b && b.StyleId == "LessonBlock").ToList();
        foreach (var child in toRemove)
        {
            LessonGrid.Children.Remove(child);
        }

        var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var diff = (7 + (viewModel.CurrentDate.Date.DayOfWeek - firstDayOfWeek)) % 7;
        var startOfWeek = viewModel.CurrentDate.Date.AddDays(-1 * diff);

        foreach (var lesson in viewModel.Lessons)
        {
            // Calculate column (1 to 7)
            int col = (lesson.StartTime.Date - startOfWeek).Days + 1;
            if (col < 1 || col > 7) continue;

            // Calculate row (0 to 11 for 6:00 to 17:00)
            int row = lesson.StartTime.Hour - 6;
            if (row < 0 || row > 11) continue;

            var border = new Border
            {
                StyleId = "LessonBlock",
                BackgroundColor = Color.FromArgb("#5d6a3f"), // Default greenish color from image
                StrokeThickness = 0,
                Margin = new Thickness(1),
                Content = new Label
                {
                    Text = lesson.WorkoutName, // Or participant count if we have it
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14
                }
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => {
                await viewModel.GoToDetailsCommand.ExecuteAsync(lesson);
            };
            border.GestureRecognizers.Add(tapGesture);

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            LessonGrid.Children.Add(border);
        }
    }
}
