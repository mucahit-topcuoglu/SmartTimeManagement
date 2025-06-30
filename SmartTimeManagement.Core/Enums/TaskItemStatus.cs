namespace SmartTimeManagement.Core.Enums;

public enum TaskItemStatus
{
    NotStarted = 0,      // Başlamadı
    InProgress = 1,      // Devam Ediyor
    Completed = 2,       // Tamamlandı
    Cancelled = 3,       // İptal Edildi
    OnHold = 4          // Beklemede
}
