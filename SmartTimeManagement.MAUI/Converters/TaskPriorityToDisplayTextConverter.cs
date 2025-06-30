using System.Globalization;
using SmartTimeManagement.Core.Enums;

namespace SmartTimeManagement.MAUI.Converters;

public class TaskPriorityToDisplayTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => "Düşük",
                TaskPriority.Medium => "Orta",
                TaskPriority.High => "Yüksek",
                TaskPriority.Critical => "Kritik",
                _ => priority.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text switch
            {
                "Düşük" => TaskPriority.Low,
                "Orta" => TaskPriority.Medium,
                "Yüksek" => TaskPriority.High,
                "Kritik" => TaskPriority.Critical,
                _ => TaskPriority.Medium
            };
        }
        return TaskPriority.Medium;
    }
}
