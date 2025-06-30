using SmartTimeManagement.Core.Entities;

namespace SmartTimeManagement.Core.Interfaces;

public interface IReminderService
{
    Task<Reminder> CreateAsync(Reminder reminder);
    Task<Reminder?> GetByIdAsync(int id);
    Task<IEnumerable<Reminder>> GetAllAsync();
    Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Reminder>> GetActiveRemindersAsync(int userId);
    Task<IEnumerable<Reminder>> GetDueRemindersAsync();
    Task<Reminder> UpdateAsync(Reminder reminder);
    Task<bool> DeleteAsync(int id);
    Task<Reminder> MarkAsCompletedAsync(int reminderId, string updatedBy);
}
