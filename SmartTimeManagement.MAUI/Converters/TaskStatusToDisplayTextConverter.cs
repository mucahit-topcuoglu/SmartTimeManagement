using System.Globalization;
using SmartTimeManagement.Core.Enums;

namespace SmartTimeManagement.MAUI.Converters;

public class TaskStatusToDisplayTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskItemStatus status)
        {
            return status switch
            {
                TaskItemStatus.NotStarted => "Başlamadı",
                TaskItemStatus.InProgress => "Devam Ediyor",
                TaskItemStatus.Completed => "Tamamlandı",
                TaskItemStatus.Cancelled => "İptal Edildi",
                TaskItemStatus.OnHold => "Beklemede",
                _ => status.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
