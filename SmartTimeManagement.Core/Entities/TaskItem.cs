using SmartTimeManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.Core.Entities;

public class TaskItem
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    
    public TaskItemStatus Status { get; set; } = TaskItemStatus.NotStarted;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public TimeSpan? EstimatedDuration { get; set; }
    
    public TimeSpan? ActualDuration { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Timer Properties
    public bool IsTimerRunning { get; set; } = false;
    
    public DateTime? TimerStartedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    // Foreign Keys
    public int UserId { get; set; }
    
    public int CategoryId { get; set; }
    
    // Navigation Properties
    public virtual User User { get; set; } = null!;
    
    public virtual Category Category { get; set; } = null!;
    
    public virtual ICollection<TaskTimeLog> TimeLogs { get; set; } = new List<TaskTimeLog>();
}
