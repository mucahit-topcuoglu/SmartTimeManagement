using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;

namespace SmartTimeManagement.Core.Interfaces;

public interface ITaskService
{
    Task<TaskItem> CreateAsync(TaskItem task);
    Task<TaskItem?> GetByIdAsync(int id);
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId);
    Task<IEnumerable<TaskItem>> GetByCategoryIdAsync(int categoryId);
    Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskItemStatus status);
    Task<IEnumerable<TaskItem>> GetByPriorityAsync(TaskPriority priority);
    Task<IEnumerable<TaskItem>> GetTasksDueTodayAsync(int userId);
    Task<IEnumerable<TaskItem>> GetOverdueTasksAsync(int userId);
    Task<TaskItem> UpdateAsync(TaskItem task);
    Task<bool> DeleteAsync(int id);
    Task<TaskItem> UpdateStatusAsync(int taskId, TaskItemStatus status, string updatedBy);
    Task<IEnumerable<TaskItem>> SearchTasksAsync(string searchTerm, int userId);
    Task<IEnumerable<TaskItem>> GetTasksByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    // Timer Methods
    Task<TaskItem> StartTimerAsync(int taskId);
    Task<TaskItem> StopTimerAsync(int taskId);
    Task<TaskItem> CompleteTaskAsync(int taskId, bool isCompleted);
}
