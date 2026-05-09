using System.Globalization;

namespace FitLife.Maui.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Ingecheckt" => Colors.Green,
                "Gereserveerd" => Colors.Orange,
                "Geannuleerd" => Colors.Red,
                _ => Colors.Black
            };
        }
        return Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
