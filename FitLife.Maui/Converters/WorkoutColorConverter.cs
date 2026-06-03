using System.Globalization;
using SharedLibrary.DTOs.Responses;

namespace FitLife.Maui.Converters;

// Converts a workout color value (hex string or workout name) to a MAUI Color object.
// Used in XAML to colour lesson cards on the schedule.
// Priority: (1) hex string from DB → parse directly,
//           (2) known workout name → look up in resource dictionary,
//           (3) fallback to app's Primary colour.
public class WorkoutColorConverter : IValueConverter
{
    // Convert: takes the string value bound from the ViewModel and returns a Color.
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
        {
            // If the string already is a hex colour (e.g. "#5B6636"), parse it directly
            if (s.StartsWith('#'))
            {
                try { return Color.FromArgb(s); } catch { }
            }

            // Map well-known workout names to a named colour defined in the App resources
            var key = s.ToLower() switch
            {
                var n when n.Contains("crossfit")   => "WorkoutCrossfit",
                var n when n.Contains("spinning")   => "WorkoutSpinning",
                var n when n.Contains("sweat club") => "WorkoutSweatClub",
                var n when n.Contains("open gym")   => "WorkoutOpenGym",
                var n when n.Contains("hyrox")      => "WorkoutHyrox",
                var n when n.Contains("gymnastics") => "WorkoutGymnastics",
                _                                   => "Primary"
            };

            // Try to resolve the colour key from the application's merged resource dictionaries
            if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
                return (Color)color;
        }

        // Ultimate fallback: use the app's primary brand colour
        return Application.Current?.Resources["Primary"] as Color;
    }

    // ConvertBack is not needed because colour is only used for display, not for editing.
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
