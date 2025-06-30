using SmartTimeManagement.Core.Entities;

namespace SmartTimeManagement.Core.Interfaces;

public interface ITaskTimeLogService
{
    Task<TaskTimeLog> StartTimerAsync(int taskId, string startedBy);
    Task<TaskTimeLog> StopTimerAsync(int taskId, string notes = "");
    Task<TaskTimeLog> CreateAsync(TaskTimeLog timeLog);
    Task<TaskTimeLog?> GetByIdAsync(int id);
    Task<IEnumerable<TaskTimeLog>> GetByTaskIdAsync(int taskId);
    Task<IEnumerable<TaskTimeLog>> GetByDateRangeAsync(int taskId, DateTime startDate, DateTime endDate);
    Task<TaskTimeLog> UpdateAsync(TaskTimeLog timeLog);
    Task<bool> DeleteAsync(int id);
    Task<TimeSpan> GetTotalTimeSpentAsync(int taskId);
    Task<TimeSpan> GetTotalTimeSpentByUserAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<TaskTimeLog>> GetTimeLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<TaskTimeLog>> GetTimeLogsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
}
