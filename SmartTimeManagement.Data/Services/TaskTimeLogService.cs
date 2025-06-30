using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Interfaces;
using SmartTimeManagement.Data.Context;

namespace SmartTimeManagement.Data.Services;

public class TaskTimeLogService : ITaskTimeLogService
{
    private readonly SmartTimeManagementDbContext _context;

    public TaskTimeLogService(SmartTimeManagementDbContext context)
    {
        _context = context;
    }

    public async Task<TaskTimeLog> StartTimerAsync(int taskId, string startedBy)
    {
        // Önce aynı görev için çalışan zamanlayıcı var mı kontrol et
        var existingTimer = await _context.TaskTimeLogs
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.EndTime == null);

        if (existingTimer != null)
        {
            throw new InvalidOperationException("Bu görev için zaten çalışan bir zamanlayıcı var.");
        }

        var timeLog = new TaskTimeLog
        {
            TaskId = taskId,
            StartTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = startedBy
        };

        _context.TaskTimeLogs.Add(timeLog);
        await _context.SaveChangesAsync();

        return timeLog;
    }

    public async Task<TaskTimeLog> StopTimerAsync(int taskId, string notes = "")
    {
        var timeLog = await _context.TaskTimeLogs
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.EndTime == null);

        if (timeLog == null)
        {
            throw new InvalidOperationException("Bu görev için çalışan bir zamanlayıcı bulunamadı.");
        }

        timeLog.EndTime = DateTime.UtcNow;
        timeLog.Duration = timeLog.EndTime.Value - timeLog.StartTime;
        timeLog.Notes = notes;

        await _context.SaveChangesAsync();

        // Görevin toplam süresini güncelle
        var task = await _context.Tasks.FindAsync(taskId);
        if (task != null)
        {
            var totalTime = await GetTotalTimeSpentAsync(taskId);
            task.ActualDuration = totalTime;
            await _context.SaveChangesAsync();
        }

        return timeLog;
    }

    public async Task<TaskTimeLog> CreateAsync(TaskTimeLog timeLog)
    {
        timeLog.CreatedAt = DateTime.UtcNow;
        timeLog.Duration = timeLog.EndTime.HasValue ? timeLog.EndTime.Value - timeLog.StartTime : TimeSpan.Zero;
        
        _context.TaskTimeLogs.Add(timeLog);
        await _context.SaveChangesAsync();
        
        return timeLog;
    }

    public async Task<TaskTimeLog?> GetByIdAsync(int id)
    {
        return await _context.TaskTimeLogs
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<TaskTimeLog>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TaskTimeLogs
            .Where(t => t.TaskId == taskId)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskTimeLog>> GetByDateRangeAsync(int taskId, DateTime startDate, DateTime endDate)
    {
        return await _context.TaskTimeLogs
            .Where(t => t.TaskId == taskId && 
                       t.StartTime >= startDate && 
                       t.StartTime <= endDate)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskTimeLog>> GetTimeLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.TaskTimeLogs
            .Include(t => t.Task)
            .Where(t => t.StartTime >= startDate && t.StartTime <= endDate)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskTimeLog>> GetTimeLogsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.TaskTimeLogs
            .Include(t => t.Task)
            .Where(t => t.Task.UserId == userId && 
                       t.StartTime >= startDate && 
                       t.StartTime <= endDate)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<TaskTimeLog> UpdateAsync(TaskTimeLog timeLog)
    {
        timeLog.Duration = timeLog.EndTime.HasValue ? timeLog.EndTime.Value - timeLog.StartTime : TimeSpan.Zero;
        
        _context.TaskTimeLogs.Update(timeLog);
        await _context.SaveChangesAsync();
        
        return timeLog;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var timeLog = await _context.TaskTimeLogs.FindAsync(id);
        if (timeLog != null)
        {
            _context.TaskTimeLogs.Remove(timeLog);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<TimeSpan> GetTotalTimeSpentAsync(int taskId)
    {
        var timeLogs = await _context.TaskTimeLogs
            .Where(t => t.TaskId == taskId && t.EndTime.HasValue)
            .ToListAsync();

        return timeLogs.Aggregate(TimeSpan.Zero, (sum, log) => sum + log.Duration);
    }

    public async Task<TimeSpan> GetTotalTimeSpentByUserAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var timeLogs = await _context.TaskTimeLogs
            .Include(t => t.Task)
            .Where(t => t.Task.UserId == userId && 
                       t.StartTime >= startDate && 
                       t.StartTime <= endDate && 
                       t.EndTime.HasValue)
            .ToListAsync();

        return timeLogs.Aggregate(TimeSpan.Zero, (sum, log) => sum + log.Duration);
    }
}
