using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Interfaces;
using SmartTimeManagement.Core.Enums;
using SmartTimeManagement.Data.Context;

namespace SmartTimeManagement.Data.Services;

public class TaskService : ITaskService
{
    private readonly SmartTimeManagementDbContext _context;

    public TaskService(SmartTimeManagementDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        task.CreatedAt = DateTime.UtcNow;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.Category)
            .Include(t => t.TimeLogs)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByCategoryIdAsync(int categoryId)
    {
        return await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.Category)
            .Where(t => t.CategoryId == categoryId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(TaskItemStatus status)
    {
        return await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.Category)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByPriorityAsync(TaskPriority priority)
    {
        return await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.Category)
            .Where(t => t.Priority == priority)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksDueTodayAsync(int userId)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        return await _context.Tasks
            .Where(t => t.UserId == userId && 
                       t.DueDate >= today && 
                       t.DueDate < tomorrow && 
                       t.Status != TaskItemStatus.Completed)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync(int userId)
    {
        var today = DateTime.Today;

        return await _context.Tasks
            .Where(t => t.UserId == userId && 
                       t.DueDate < today && 
                       t.Status != TaskItemStatus.Completed)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<TaskItem> UpdateStatusAsync(int taskId, TaskItemStatus status, string updatedBy)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            throw new ArgumentException("Görev bulunamadı.");
        }

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = updatedBy;

        if (status == TaskItemStatus.Completed)
        {
            task.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<IEnumerable<TaskItem>> SearchTasksAsync(string searchTerm, int userId)
    {
        return await _context.Tasks
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && 
                       (t.Title.Contains(searchTerm) || 
                        (t.Description != null && t.Description.Contains(searchTerm))))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Tasks
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && 
                       t.CreatedAt >= startDate && 
                       t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Tasks
            .Include(t => t.Category)
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem> StartTimerAsync(int taskId)
    {
        Console.WriteLine($"TaskService.StartTimerAsync: TaskId={taskId} aranıyor...");
        var task = await GetByIdAsync(taskId);
        if (task == null)
        {
            Console.WriteLine($"TaskService.StartTimerAsync: TaskId={taskId} bulunamadı!");
            throw new ArgumentException("Görev bulunamadı", nameof(taskId));
        }

        Console.WriteLine($"TaskService.StartTimerAsync: TaskId={taskId} bulundu - Title='{task.Title}', IsTimerRunning={task.IsTimerRunning}");

        if (task.IsTimerRunning)
            throw new InvalidOperationException("Bu görev için timer zaten çalışıyor");

        task.IsTimerRunning = true;
        task.TimerStartedAt = DateTime.UtcNow;
        task.Status = TaskItemStatus.InProgress;
        task.StartDate = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(task);
        Console.WriteLine($"TaskService.StartTimerAsync: TaskId={taskId} timer başlatıldı");
        return task;
    }

    public async Task<TaskItem> StopTimerAsync(int taskId)
    {
        var task = await GetByIdAsync(taskId);
        if (task == null)
            throw new ArgumentException("Görev bulunamadı", nameof(taskId));

        if (!task.IsTimerRunning)
            throw new InvalidOperationException("Bu görev için timer çalışmıyor");

        var elapsed = DateTime.UtcNow - task.TimerStartedAt!.Value;
        
        task.IsTimerRunning = false;
        task.TimerStartedAt = null;
        task.ActualDuration = (task.ActualDuration ?? TimeSpan.Zero) + elapsed;
        task.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(task);
        return task;
    }

    public async Task<TaskItem> CompleteTaskAsync(int taskId, bool isCompleted)
    {
        var task = await GetByIdAsync(taskId);
        if (task == null)
            throw new ArgumentException("Görev bulunamadı", nameof(taskId));

        // Eğer timer çalışıyorsa önce durdur
        if (task.IsTimerRunning)
        {
            await StopTimerAsync(taskId);
            task = await GetByIdAsync(taskId); // Güncel halini al
        }

        task.Status = isCompleted ? TaskItemStatus.Completed : TaskItemStatus.NotStarted;
        task.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(task);
        return task;
    }

    public TimeSpan GetCurrentTimerDuration(TaskItem task)
    {
        if (!task.IsTimerRunning || !task.TimerStartedAt.HasValue)
            return TimeSpan.Zero;

        return DateTime.UtcNow - task.TimerStartedAt.Value;
    }
}
