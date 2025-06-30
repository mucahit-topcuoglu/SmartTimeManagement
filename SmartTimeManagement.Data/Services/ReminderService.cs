using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Interfaces;
using SmartTimeManagement.Data.Context;

namespace SmartTimeManagement.Data.Services;

public class ReminderService : IReminderService
{
    private readonly SmartTimeManagementDbContext _context;

    public ReminderService(SmartTimeManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Reminder> CreateAsync(Reminder reminder)
    {
        reminder.CreatedAt = DateTime.UtcNow;
        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<Reminder?> GetByIdAsync(int id)
    {
        return await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Task)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reminder>> GetAllAsync()
    {
        return await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Task)
            .OrderByDescending(r => r.ReminderDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetByUserIdAsync(int userId)
    {
        return await _context.Reminders
            .Include(r => r.Task)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReminderDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetActiveRemindersAsync(int userId)
    {
        return await _context.Reminders
            .Include(r => r.Task)
            .Where(r => r.UserId == userId && r.IsActive && !r.IsCompleted)
            .OrderBy(r => r.ReminderDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetDueRemindersAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Reminders
            .Include(r => r.User)
            .Include(r => r.Task)
            .Where(r => r.IsActive && 
                       !r.IsCompleted && 
                       r.ReminderDateTime <= now)
            .ToListAsync();
    }

    public async Task<Reminder> UpdateAsync(Reminder reminder)
    {
        reminder.UpdatedAt = DateTime.UtcNow;
        _context.Reminders.Update(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var reminder = await _context.Reminders.FindAsync(id);
        if (reminder != null)
        {
            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<Reminder> MarkAsCompletedAsync(int reminderId, string updatedBy)
    {
        var reminder = await _context.Reminders.FindAsync(reminderId);
        if (reminder == null)
        {
            throw new ArgumentException("Hatırlatıcı bulunamadı.");
        }

        reminder.IsCompleted = true;
        reminder.UpdatedAt = DateTime.UtcNow;
        reminder.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
        return reminder;
    }
}
