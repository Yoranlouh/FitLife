using System.Globalization;
using FitLife.Maui.Helpers;

namespace FitLife.Maui.Converters;

// Converts a workout color hex string (from LessonResponse.WorkoutColor) to a MAUI Color.
// Delegates entirely to WorkoutColorHelper so all color resolution uses one central source.
public class WorkoutColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => WorkoutColorHelper.Resolve(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
