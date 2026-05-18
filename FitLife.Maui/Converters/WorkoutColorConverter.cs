using System.Globalization;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.Converters;

public class WorkoutColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string workoutName)
        {
            var key = workoutName.ToLower() switch
            {
                var s when s.Contains("crossfit") => "WorkoutCrossfit",
                var s when s.Contains("spinning") => "WorkoutSpinning",
                var s when s.Contains("sweat club") => "WorkoutSweatClub",
                var s when s.Contains("open gym") => "WorkoutOpenGym",
                var s when s.Contains("hyrox") => "WorkoutHyrox",
                var s when s.Contains("gymnastics") => "WorkoutGymnastics",
                _ => "Primary"
            };

            if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
            {
                return (Color)color;
            }
        }
        return Application.Current?.Resources["Primary"] as Color;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
