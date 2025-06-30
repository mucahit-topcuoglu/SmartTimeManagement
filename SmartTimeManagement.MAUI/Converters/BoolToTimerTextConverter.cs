using System.Globalization;

namespace SmartTimeManagement.MAUI.Converters;

public class BoolToTimerTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRunning)
        {
            return isRunning ? "⏱️ Durdur" : "⏱️ Başlat";
        }
        return "⏱️ Başlat";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
