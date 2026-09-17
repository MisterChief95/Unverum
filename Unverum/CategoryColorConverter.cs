using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Unverum;

public class CategoryColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return (string)value switch
        {
            "BugFix" or "Overhaul" => new SolidColorBrush(Color.FromRgb(255, 78, 78)),
            "Addition" or "Feature" => new SolidColorBrush(Color.FromRgb(108, 177, 255)),
            "Tweak" or "Improvement" or "Optimization" => new SolidColorBrush(Color.FromRgb(255, 94, 157)),
            "Adjustment" or "Suggestion" or "Ammendment" => new SolidColorBrush(Color.FromRgb(110, 255, 108)),
            "Removal" or "Refactor" => new SolidColorBrush(Color.FromRgb(153, 153, 153)),
            _ => new SolidColorBrush(Color.FromRgb(0, 0, 0))
        };
    }

    public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return null;
    }
}
