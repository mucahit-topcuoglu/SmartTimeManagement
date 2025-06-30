using SmartTimeManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.Core.Entities;

public class Report
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public ReportType Type { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public int TotalTasks { get; set; }
    
    public int CompletedTasks { get; set; }
    
    public int InProgressTasks { get; set; }
    
    public int NotStartedTasks { get; set; }
    
    public TimeSpan TotalTimeSpent { get; set; }
    
    public double ProductivityScore { get; set; }
    
    public string? ReportData { get; set; } // JSON format for detailed data
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? CreatedBy { get; set; }
    
    // Foreign Key
    public int UserId { get; set; }
    
    // Navigation Property
    public virtual User User { get; set; } = null!;
}
