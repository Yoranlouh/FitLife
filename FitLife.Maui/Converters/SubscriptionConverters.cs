using System.Globalization;

namespace FitLife.Maui.Converters;

/// <summary>
/// Converts a yearly price to monthly price (divides by 12 and rounds to 2 decimals)
/// </summary>
public class YearlyToMonthlyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal yearlyPrice)
        {
            var monthlyPrice = Math.Round(yearlyPrice / 12, 2);
            return monthlyPrice.ToString("F2");
        }
        return "0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts inverse boolean to FontAttributes (Bold if true, None if false)
/// </summary>
public class InverseBoolToFontAttributesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? FontAttributes.None : FontAttributes.Bold;
        }
        return FontAttributes.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to FontAttributes (Bold if true, None if false)
/// </summary>
public class BoolToFontAttributesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? FontAttributes.Bold : FontAttributes.None;
        }
        return FontAttributes.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts inverse boolean to Color (Primary if true, Gray if false)
/// </summary>
public class InverseBoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Colors.Gray : Color.FromArgb("#5B6636"); // Primary color
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts subscription name to boolean for showing drink feature
/// Returns true if name is Intermediate or Advanced
/// </summary>
public class NameToShowDrinkConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string name)
        {
            return name == "Intermediate" || name == "Advanced";
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts subscription name to boolean for Advanced-only features
/// Returns true if name is Advanced
/// </summary>
public class NameIsAdvancedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string name)
        {
            return name == "Advanced";
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
