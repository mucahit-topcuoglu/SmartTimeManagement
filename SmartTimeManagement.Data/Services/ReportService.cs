using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Interfaces;
using SmartTimeManagement.Core.Enums;
using SmartTimeManagement.Data.Context;
using System.Text.Json;

namespace SmartTimeManagement.Data.Services;

public class ReportService : IReportService
{
    private readonly SmartTimeManagementDbContext _context;

    public ReportService(SmartTimeManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Report> GenerateDailyReportAsync(int userId, DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        return await GenerateReportAsync(userId, startDate, endDate, ReportType.Daily, $"Günlük Rapor - {date:dd.MM.yyyy}");
    }

    public async Task<Report> GenerateWeeklyReportAsync(int userId, DateTime startDate)
    {
        var endDate = startDate.AddDays(7);

        return await GenerateReportAsync(userId, startDate, endDate, ReportType.Weekly, $"Haftalık Rapor - {startDate:dd.MM.yyyy}");
    }

    public async Task<Report> GenerateMonthlyReportAsync(int userId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await GenerateReportAsync(userId, startDate, endDate, ReportType.Monthly, $"Aylık Rapor - {startDate:MM.yyyy}");
    }

    public async Task<Report> GenerateCustomReportAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await GenerateReportAsync(userId, startDate, endDate, ReportType.Custom, $"Özel Rapor - {startDate:dd.MM.yyyy} / {endDate:dd.MM.yyyy}");
    }

    private async Task<Report> GenerateReportAsync(int userId, DateTime startDate, DateTime endDate, ReportType reportType, string title)
    {
        var tasks = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.TimeLogs)
            .Where(t => t.UserId == userId && t.CreatedAt >= startDate && t.CreatedAt < endDate)
            .ToListAsync();

        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == TaskItemStatus.Completed);
        var inProgressTasks = tasks.Count(t => t.Status == TaskItemStatus.InProgress);
        var notStartedTasks = tasks.Count(t => t.Status == TaskItemStatus.NotStarted);

        var totalTimeSpent = tasks
            .SelectMany(t => t.TimeLogs)
            .Aggregate(TimeSpan.Zero, (sum, log) => sum + log.Duration);

        var productivityScore = await CalculateProductivityScoreAsync(userId, startDate, endDate);

        var reportData = await GetProductivityStatisticsAsync(userId, startDate, endDate);

        var report = new Report
        {
            Title = title,
            Type = reportType,
            StartDate = startDate,
            EndDate = endDate,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            InProgressTasks = inProgressTasks,
            NotStartedTasks = notStartedTasks,
            TotalTimeSpent = totalTimeSpent,
            ProductivityScore = productivityScore,
            ReportData = JsonSerializer.Serialize(reportData),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        return report;
    }

    public async Task<Report?> GetByIdAsync(int id)
    {
        return await _context.Reports
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Report>> GetByUserIdAsync(int userId)
    {
        return await _context.Reports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Report> SaveReportAsync(Report report)
    {
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report != null)
        {
            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<double> CalculateProductivityScoreAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId && t.CreatedAt >= startDate && t.CreatedAt < endDate)
            .ToListAsync();

        if (tasks.Count == 0)
            return 0;

        var completedTasks = tasks.Count(t => t.Status == TaskItemStatus.Completed);
        var completionRate = (double)completedTasks / tasks.Count;

        var onTimeCompletions = tasks
            .Where(t => t.Status == TaskItemStatus.Completed && 
                       t.CompletedAt.HasValue && 
                       t.DueDate.HasValue && 
                       t.CompletedAt <= t.DueDate)
            .Count();

        var onTimeRate = tasks.Count(t => t.DueDate.HasValue) > 0 
            ? (double)onTimeCompletions / tasks.Count(t => t.DueDate.HasValue)
            : 1.0;

        // Üretkenlik skoru = (Tamamlama oranı * 0.7) + (Zamanında tamamlama oranı * 0.3)
        var productivityScore = (completionRate * 0.7) + (onTimeRate * 0.3);

        return Math.Round(productivityScore * 100, 2);
    }

    public async Task<Dictionary<string, object>> GetProductivityStatisticsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var tasks = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.TimeLogs)
            .Where(t => t.UserId == userId && t.CreatedAt >= startDate && t.CreatedAt < endDate)
            .ToListAsync();

        var categoryStats = tasks
            .GroupBy(t => t.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                TaskCount = g.Count(),
                CompletedCount = g.Count(t => t.Status == TaskItemStatus.Completed),
                TotalTime = g.SelectMany(t => t.TimeLogs).Aggregate(TimeSpan.Zero, (sum, log) => sum + log.Duration)
            })
            .ToList();

        var priorityStats = tasks
            .GroupBy(t => t.Priority)
            .Select(g => new
            {
                Priority = g.Key.ToString(),
                TaskCount = g.Count(),
                CompletedCount = g.Count(t => t.Status == TaskItemStatus.Completed)
            })
            .ToList();

        var dailyProgress = Enumerable.Range(0, (endDate - startDate).Days)
            .Select(i =>
            {
                var day = startDate.AddDays(i);
                var dayTasks = tasks.Where(t => t.CreatedAt.Date == day.Date).ToList();
                return new
                {
                    Date = day.ToString("dd.MM.yyyy"),
                    TasksCreated = dayTasks.Count,
                    TasksCompleted = dayTasks.Count(t => t.Status == TaskItemStatus.Completed),
                    TimeSpent = dayTasks.SelectMany(t => t.TimeLogs).Aggregate(TimeSpan.Zero, (sum, log) => sum + log.Duration).TotalHours
                };
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["CategoryStatistics"] = categoryStats,
            ["PriorityStatistics"] = priorityStats,
            ["DailyProgress"] = dailyProgress,
            ["AverageTaskCompletionTime"] = tasks
                .Where(t => t.Status == TaskItemStatus.Completed && t.CompletedAt.HasValue)
                .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average(),
            ["MostProductiveDay"] = dailyProgress.OrderByDescending(d => d.TasksCompleted).FirstOrDefault()?.Date ?? "Veri yok"
        };
    }

    public async Task<IEnumerable<Report>> GetRecentReportsAsync()
    {
        return await _context.Reports
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();
    }
}
