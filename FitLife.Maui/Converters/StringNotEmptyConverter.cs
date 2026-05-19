using System.Globalization;

namespace FitLife.Maui.Converters;

/// <summary>
/// Converter that returns true if a string is not null or empty
/// Used to show/hide UI elements based on string content
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
