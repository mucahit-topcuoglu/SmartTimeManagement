using SmartTimeManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.Core.Entities;

public class Reminder
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public DateTime ReminderDateTime { get; set; }
    
    public ReminderType Type { get; set; } = ReminderType.OneTime;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsCompleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    // Foreign Keys
    public int UserId { get; set; }
    
    public int? TaskId { get; set; }
    
    // Navigation Properties
    public virtual User User { get; set; } = null!;
    
    public virtual TaskItem? Task { get; set; }
}
