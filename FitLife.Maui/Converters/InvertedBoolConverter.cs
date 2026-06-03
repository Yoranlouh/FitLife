using System.Globalization;

namespace FitLife.Maui.Converters;

// Value converter that flips a boolean: true → false, false → true.
// Used in XAML bindings like IsEnabled="{Binding IsBusy, Converter={StaticResource InvertedBoolConverter}}"
// to disable a button while a background operation is running.
public class InvertedBoolConverter : IValueConverter
{
    // Converts bool to its inverse. Returns true (safe default) for non-bool input.
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    // Two-way binding support: applies the same inversion in the opposite direction.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }
}
