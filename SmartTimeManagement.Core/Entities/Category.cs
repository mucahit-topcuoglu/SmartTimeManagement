using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.Core.Entities;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(7)]
    public string Color { get; set; } = "#007ACC"; // Varsayılan renk
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    // Navigation Properties
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
