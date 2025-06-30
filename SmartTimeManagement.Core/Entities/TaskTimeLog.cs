using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.Core.Entities;

public class TaskTimeLog
{
    public int Id { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan Duration { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? CreatedBy { get; set; }
    
    // Foreign Key
    public int TaskId { get; set; }
    
    // Navigation Property
    public virtual TaskItem Task { get; set; } = null!;
}
