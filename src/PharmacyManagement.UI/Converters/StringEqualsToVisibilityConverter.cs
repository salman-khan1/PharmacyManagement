using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PharmacyManagement.UI.Converters;

public class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var target = parameter?.ToString() ?? string.Empty;
        var str = value as string;

        // Show when value is null/empty or equals target
        if (string.IsNullOrEmpty(str) || string.Equals(str, target, StringComparison.OrdinalIgnoreCase))
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
