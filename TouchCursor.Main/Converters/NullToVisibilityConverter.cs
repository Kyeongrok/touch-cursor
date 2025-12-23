using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TouchCursor.Main.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // null이면 Visible, 값이 있으면 Collapsed (Title은 TabContent가 없을 때만 표시)
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
