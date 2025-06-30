using SmartTimeManagement.Core.Entities;

namespace SmartTimeManagement.Core.Interfaces;

public interface IReportService
{
    Task<Report> GenerateDailyReportAsync(int userId, DateTime date);
    Task<Report> GenerateWeeklyReportAsync(int userId, DateTime startDate);
    Task<Report> GenerateMonthlyReportAsync(int userId, int year, int month);
    Task<Report> GenerateCustomReportAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Report?> GetByIdAsync(int id);
    Task<IEnumerable<Report>> GetByUserIdAsync(int userId);
    Task<Report> SaveReportAsync(Report report);
    Task<bool> DeleteAsync(int id);
    Task<double> CalculateProductivityScoreAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Dictionary<string, object>> GetProductivityStatisticsAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Report>> GetRecentReportsAsync();
}
