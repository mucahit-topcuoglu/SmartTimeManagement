using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;

namespace SmartTimeManagement.Data.Context;

public class SmartTimeManagementDbContext : DbContext
{
    public SmartTimeManagementDbContext(DbContextOptions<SmartTimeManagementDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskTimeLog> TaskTimeLogs { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).HasConversion<int>();
        });

        // Category Configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Color).HasMaxLength(7);
        });

        // TaskItem Configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Priority).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TaskTimeLog Configuration
        modelBuilder.Entity<TaskTimeLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.Task)
                .WithMany(t => t.TimeLogs)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Reminder Configuration
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type).HasConversion<int>();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
                
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Report Configuration
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.ProductivityScore).HasPrecision(5, 2);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "İş", Description = "İş ile ilgili görevler", Color = "#FF6B35", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Kişisel", Description = "Kişisel görevler", Color = "#007ACC", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Spor", Description = "Spor ve sağlık ile ilgili görevler", Color = "#28A745", CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Eğitim", Description = "Öğrenme ve gelişim görevleri", Color = "#6F42C1", CreatedAt = DateTime.UtcNow },
            new Category { Id = 5, Name = "Alışveriş", Description = "Alışveriş listesi", Color = "#FD7E14", CreatedAt = DateTime.UtcNow }
        );

        // Seed Admin User
        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                Id = 1, 
                FirstName = "Admin", 
                LastName = "User", 
                Email = "admin@smarttime.com", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), 
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow 
            }
        );
    }
}
