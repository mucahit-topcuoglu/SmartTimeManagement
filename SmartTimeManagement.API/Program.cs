using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Data.Context;
using SmartTimeManagement.Core.Interfaces;
using SmartTimeManagement.Data.Services;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

using TaskPriority = SmartTimeManagement.Core.Enums.TaskPriority;
using TaskItemStatus = SmartTimeManagement.Core.Enums.TaskItemStatus;
using UserRole = SmartTimeManagement.Core.Enums.UserRole;
using ReminderType = SmartTimeManagement.Core.Enums.ReminderType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JSON Serialization ayarları
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Database Configuration
builder.Services.AddDbContext<SmartTimeManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITaskTimeLogService, TaskTimeLogService>();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Veritabanını oluştur/güncelle
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SmartTimeManagementDbContext>();
    try
    {
        Console.WriteLine("Veritabanı oluşturuluyor...");
        
        // Veritabanının var olup olmadığını kontrol et
        bool canConnect = context.Database.CanConnect();
        Console.WriteLine($"Veritabanına bağlantı: {canConnect}");
        
        // Veritabanını oluştur (varsa oluşturmaz)
        bool created = context.Database.EnsureCreated();
        Console.WriteLine($"Veritabanı oluşturuldu: {created}");
        
        // Seed data ekle
        if (!context.Users.Any())
        {
            Console.WriteLine("Test kullanıcısı ekleniyor...");
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            await userService.CreateUserAsync("Test", "User", "test@example.com", "TestPassword123", UserRole.User);
            Console.WriteLine("Test kullanıcısı eklendi!");
        }
        else
        {
            Console.WriteLine("Test kullanıcısı zaten mevcut.");
        }

        // Test kategorisi ekle
        if (!context.Categories.Any())
        {
            Console.WriteLine("Test kategorisi ekleniyor...");
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            await categoryService.CreateAsync(new Category 
            { 
                Name = "Genel", 
                Description = "Genel görevler", 
                Color = "#007ACC",
                CreatedBy = "System"
            });
            Console.WriteLine("Test kategorisi eklendi!");
        }
        else
        {
            Console.WriteLine("Kategoriler zaten mevcut.");
        }

        // Test görevleri ekle (debug için)
        if (!context.Tasks.Any())
        {
            Console.WriteLine("Test görevleri ekleniyor...");
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            var testUser = await context.Users.FirstAsync();
            var testCategory = await context.Categories.FirstAsync();
            
            var testTask1 = new TaskItem
            {
                Title = "Test Görev 1",
                Description = "Bu bir test görevidir",
                Priority = TaskPriority.High,
                Status = TaskItemStatus.NotStarted,
                DueDate = DateTime.Now.AddDays(3),
                EstimatedDuration = TimeSpan.FromHours(2),
                UserId = testUser.Id,
                CategoryId = testCategory.Id,
                CreatedBy = "System"
            };
            
            var testTask2 = new TaskItem
            {
                Title = "Test Görev 2", 
                Description = "İkinci test görevi",
                Priority = TaskPriority.Medium,
                Status = TaskItemStatus.NotStarted,
                DueDate = DateTime.Now.AddDays(5),
                EstimatedDuration = TimeSpan.FromMinutes(90),
                UserId = testUser.Id,
                CategoryId = testCategory.Id,
                CreatedBy = "System"
            };

            await taskService.CreateAsync(testTask1);
            await taskService.CreateAsync(testTask2);
            Console.WriteLine("Test görevleri eklendi!");
        }
        else
        {
            Console.WriteLine("Görevler zaten mevcut.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Veritabanı oluşturma hatası: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// User Endpoints
app.MapPost("/api/users/login", async (LoginRequest request, IUserService userService) =>
{
    var user = await userService.AuthenticateAsync(request.Email, request.Password);
    if (user == null)
    {
        return Results.Unauthorized();
    }
    
    return Results.Ok(new { 
        Id = user.Id, 
        FirstName = user.FirstName, 
        LastName = user.LastName, 
        Email = user.Email, 
        Role = (int)user.Role 
    });
});

app.MapPost("/api/users/register", async (RegisterRequest request, IUserService userService) =>
{
    try
    {
        var user = await userService.RegisterAsync(request.FirstName, request.LastName, request.Email, request.Password);
        return Results.Ok(new { 
            Id = user.Id, 
            FirstName = user.FirstName, 
            LastName = user.LastName, 
            Email = user.Email, 
            Role = (int)user.Role 
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("/api/users/{userId}/change-password", async (int userId, ChangePasswordRequest request, IUserService userService) =>
{
    var result = await userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
    if (result)
    {
        return Results.Ok();
    }
    return Results.BadRequest("Şifre değiştirilemedi.");
});

// Task Endpoints
app.MapGet("/api/tasks/user/{userId}", async (int userId, ITaskService taskService) =>
{
    Console.WriteLine($"API: Kullanıcı {userId} için görevler isteniyor...");
    
    // Önce tüm görevleri kontrol et (debug için)
    var allTasks = await taskService.GetAllAsync();
    Console.WriteLine($"API DEBUG: Toplam {allTasks.Count()} görev var");
    foreach (var task in allTasks)
    {
        Console.WriteLine($"API DEBUG: Görev ID={task.Id}, Title='{task.Title}', UserId={task.UserId}");
    }
    
    // Kullanıcıya özel görevleri al
    var userTasks = await taskService.GetByUserIdAsync(userId);
    Console.WriteLine($"API: Kullanıcı {userId} için {userTasks.Count()} görev bulundu");
    
    foreach (var task in userTasks)
    {
        Console.WriteLine($"API: Kullanıcı görevi ID={task.Id}, Title='{task.Title}', UserId={task.UserId}, DueDate={task.DueDate}");
    }
    
    return Results.Ok(userTasks);
});

app.MapGet("/api/tasks/{id}", async (int id, ITaskService taskService) =>
{
    var task = await taskService.GetByIdAsync(id);
    if (task == null) return Results.NotFound();
    return Results.Ok(task);
});

app.MapPost("/api/tasks", async (CreateTaskRequest request, ITaskService taskService) =>
{
    Console.WriteLine($"Yeni görev oluşturuluyor: {request.Title}, UserId: {request.UserId}");
    var task = new TaskItem
    {
        Title = request.Title,
        Description = request.Description,
        Priority = request.Priority,
        DueDate = request.DueDate,
        EstimatedDuration = request.EstimatedDuration,
        UserId = request.UserId,
        CategoryId = request.CategoryId,
        CreatedBy = request.CreatedBy
    };
    
    var createdTask = await taskService.CreateAsync(task);
    Console.WriteLine($"Görev oluşturuldu: ID={createdTask.Id}, UserId={createdTask.UserId}");
    return Results.Created($"/api/tasks/{createdTask.Id}", createdTask);
});

app.MapPut("/api/tasks/{id}", async (int id, UpdateTaskRequest request, ITaskService taskService) =>
{
    Console.WriteLine($"API: Görev güncelleme isteği - ID: {id}, Title: {request.Title}");
    
    var existingTask = await taskService.GetByIdAsync(id);
    if (existingTask == null) 
    {
        Console.WriteLine($"API: ID {id} ile görev bulunamadı");
        return Results.NotFound();
    }
    
    Console.WriteLine($"API: Mevcut görev bulundu - Title: {existingTask.Title}, UserId: {existingTask.UserId}");
    
    existingTask.Title = request.Title;
    existingTask.Description = request.Description;
    existingTask.Priority = request.Priority;
    existingTask.DueDate = request.DueDate;
    existingTask.EstimatedDuration = request.EstimatedDuration;
    existingTask.CategoryId = request.CategoryId;
    existingTask.UpdatedBy = request.UpdatedBy;
    existingTask.UpdatedAt = DateTime.Now;
    
    Console.WriteLine($"API: Güncelleme öncesi - Title: {existingTask.Title}, Priority: {existingTask.Priority}, Status: {existingTask.Status}");
    
    var updatedTask = await taskService.UpdateAsync(existingTask);
    
    Console.WriteLine($"API: Görev başarıyla güncellendi - ID: {updatedTask.Id}, Title: {updatedTask.Title}");
    
    return Results.Ok(updatedTask);
});

app.MapPut("/api/tasks/{id}/status", async (int id, UpdateTaskStatusRequest request, ITaskService taskService) =>
{
    try
    {
        var task = await taskService.UpdateStatusAsync(id, request.Status, request.UpdatedBy);
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapDelete("/api/tasks/{id}", async (int id, ITaskService taskService) =>
{
    var result = await taskService.DeleteAsync(id);
    if (result) return Results.Ok();
    return Results.NotFound();
});

app.MapGet("/api/tasks/user/{userId}/due-today", async (int userId, ITaskService taskService) =>
{
    var tasks = await taskService.GetTasksDueTodayAsync(userId);
    return Results.Ok(tasks);
});

app.MapGet("/api/tasks/user/{userId}/overdue", async (int userId, ITaskService taskService) =>
{
    var tasks = await taskService.GetOverdueTasksAsync(userId);
    return Results.Ok(tasks);
});

app.MapGet("/api/tasks/user/{userId}/date-range", async (int userId, DateTime start, DateTime end, ITaskService taskService) =>
{
    var tasks = await taskService.GetTasksByDateRangeAsync(userId, start, end);
    return Results.Ok(tasks);
});

// Category Endpoints
app.MapGet("/api/categories", async (ICategoryService categoryService) =>
{
    var categories = await categoryService.GetActiveAsync();
    return Results.Ok(categories);
});

app.MapPost("/api/categories", async (CreateCategoryRequest request, ICategoryService categoryService) =>
{
    var category = new Category
    {
        Name = request.Name,
        Description = request.Description,
        Color = request.Color,
        CreatedBy = request.CreatedBy
    };
    
    var createdCategory = await categoryService.CreateAsync(category);
    return Results.Created($"/api/categories/{createdCategory.Id}", createdCategory);
});

// Reminder Endpoints
app.MapGet("/api/reminders/user/{userId}", async (int userId, IReminderService reminderService) =>
{
    Console.WriteLine($"API: Kullanıcı {userId} için hatırlatıcılar isteniyor...");
    
    // Önce tüm hatırlatıcıları kontrol et (debug için)
    var allReminders = await reminderService.GetAllAsync();
    Console.WriteLine($"API DEBUG: Toplam {allReminders.Count()} hatırlatıcı var");
    foreach (var reminder in allReminders)
    {
        Console.WriteLine($"API DEBUG: Hatırlatıcı ID={reminder.Id}, Title='{reminder.Title}', UserId={reminder.UserId}");
    }
    
    // Kullanıcıya özel hatırlatıcıları al
    var userReminders = await reminderService.GetActiveRemindersAsync(userId);
    Console.WriteLine($"API: Kullanıcı {userId} için {userReminders.Count()} hatırlatıcı bulundu");
    
    foreach (var reminder in userReminders)
    {
        Console.WriteLine($"API: Kullanıcı hatırlatıcısı ID={reminder.Id}, Title='{reminder.Title}', UserId={reminder.UserId}, DateTime={reminder.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
    }
    
    return Results.Ok(userReminders);
});

app.MapPost("/api/reminders", async (CreateReminderRequest request, IReminderService reminderService) =>
{
    Console.WriteLine($"Yeni hatırlatıcı oluşturuluyor: {request.Title}, UserId: {request.UserId}, ReminderDateTime: {request.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
    var reminder = new Reminder
    {
        Title = request.Title,
        Description = request.Description,
        ReminderDateTime = request.ReminderDateTime,
        Type = request.Type,
        UserId = request.UserId,
        TaskId = request.TaskId,
        CreatedBy = request.CreatedBy
    };
    
    var createdReminder = await reminderService.CreateAsync(reminder);
    Console.WriteLine($"Hatırlatıcı oluşturuldu: ID={createdReminder.Id}, UserId={createdReminder.UserId}, DateTime={createdReminder.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
    return Results.Created($"/api/reminders/{createdReminder.Id}", createdReminder);
});

// Additional Reminder Endpoints
app.MapGet("/api/reminders/{id}", async (int id, IReminderService reminderService) =>
{
    var reminder = await reminderService.GetByIdAsync(id);
    if (reminder == null) return Results.NotFound();
    return Results.Ok(reminder);
});

app.MapPut("/api/reminders/{id}", async (int id, UpdateReminderRequest request, IReminderService reminderService) =>
{
    var existingReminder = await reminderService.GetByIdAsync(id);
    if (existingReminder == null) return Results.NotFound();
    
    existingReminder.Title = request.Title;
    existingReminder.Description = request.Description;
    existingReminder.ReminderDateTime = request.ReminderDateTime;
    existingReminder.Type = request.Type;
    existingReminder.TaskId = request.TaskId;
    existingReminder.IsActive = request.IsActive;
    existingReminder.UpdatedBy = request.UpdatedBy;
    
    var updatedReminder = await reminderService.UpdateAsync(existingReminder);
    return Results.Ok(updatedReminder);
});

app.MapDelete("/api/reminders/{id}", async (int id, IReminderService reminderService) =>
{
    Console.WriteLine($"Hatırlatıcı siliniyor: ID={id}");
    var result = await reminderService.DeleteAsync(id);
    if (result) 
    {
        Console.WriteLine($"Hatırlatıcı silindi: ID={id}");
        return Results.Ok();
    }
    Console.WriteLine($"Hatırlatıcı silinemedi: ID={id}");
    return Results.NotFound();
});

// Time Log Endpoints
app.MapPost("/api/tasks/{taskId}/timer/start", async (int taskId, ITaskService taskService) =>
{
    try
    {
        Console.WriteLine($"Timer başlatılıyor: TaskId={taskId}");
        var task = await taskService.StartTimerAsync(taskId);
        Console.WriteLine($"Timer başlatıldı: TaskId={taskId}, Status={task.Status}, IsTimerRunning={task.IsTimerRunning}");
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Timer başlatma hatası: {ex.Message}");
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("/api/tasks/{taskId}/timer/stop", async (int taskId, ITaskService taskService) =>
{
    try
    {
        Console.WriteLine($"Timer durduruluyor: TaskId={taskId}");
        var task = await taskService.StopTimerAsync(taskId);
        Console.WriteLine($"Timer durduruldu: TaskId={taskId}, ActualDuration={task.ActualDuration}");
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Timer durdurma hatası: {ex.Message}");
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("/api/tasks/{taskId}/complete", async (int taskId, CompleteTaskRequest request, ITaskService taskService) =>
{
    try
    {
        Console.WriteLine($"Görev tamamlanıyor: TaskId={taskId}, IsCompleted={request.IsCompleted}");
        var task = await taskService.CompleteTaskAsync(taskId, request.IsCompleted);
        Console.WriteLine($"Görev durumu güncellendi: TaskId={taskId}, Status={task.Status}");
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Görev tamamlama hatası: {ex.Message}");
        return Results.BadRequest(ex.Message);
    }
});

// Additional TimeLog Endpoints  
app.MapGet("/api/timelogs/user/{userId}/date-range", async (int userId, DateTime start, DateTime end, ITaskTimeLogService timeLogService) =>
{
    var timeLogs = await timeLogService.GetTimeLogsByDateRangeAsync(userId, start, end);
    return Results.Ok(timeLogs);
});

// Report Endpoints
app.MapPost("/api/reports/daily", async (GenerateReportRequest request, IReportService reportService) =>
{
    var report = await reportService.GenerateDailyReportAsync(request.UserId, request.Date);
    var savedReport = await reportService.SaveReportAsync(report);
    return Results.Ok(savedReport);
});

app.MapPost("/api/reports/weekly", async (GenerateReportRequest request, IReportService reportService) =>
{
    var report = await reportService.GenerateWeeklyReportAsync(request.UserId, request.Date);
    var savedReport = await reportService.SaveReportAsync(report);
    return Results.Ok(savedReport);
});

app.MapGet("/api/reports/user/{userId}", async (int userId, IReportService reportService) =>
{
    var reports = await reportService.GetByUserIdAsync(userId);
    return Results.Ok(reports);
});

// Additional Report Endpoints
app.MapGet("/api/reports/user/{userId}/recent", async (int userId, IReportService reportService) =>
{
    var reports = await reportService.GetByUserIdAsync(userId);
    return Results.Ok(reports);
});

app.MapPost("/api/reports", async (Report report, IReportService reportService) =>
{
    var savedReport = await reportService.SaveReportAsync(report);
    return Results.Created($"/api/reports/{savedReport.Id}", savedReport);
});

// Debug Endpoints (Development only)
app.MapGet("/api/debug/users", async (IUserService userService) =>
{
    var users = await userService.GetAllAsync();
    return Results.Ok(users.Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.Role }));
});

app.MapGet("/api/debug/tasks", async (ITaskService taskService) =>
{
    var tasks = await taskService.GetAllAsync();
    return Results.Ok(tasks.Select(t => new { t.Id, t.Title, t.UserId, t.Priority, t.Status, t.DueDate }));
});

app.Run();

// Request/Response Models
public record LoginRequest([Required] string Email, [Required] string Password);
public record RegisterRequest([Required] string FirstName, [Required] string LastName, [Required] string Email, [Required] string Password);
public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);
public record CreateTaskRequest([Required] string Title, string? Description, TaskPriority Priority, DateTime? DueDate, TimeSpan? EstimatedDuration, int UserId, int CategoryId, string? CreatedBy);
public record UpdateTaskRequest([Required] string Title, string? Description, TaskPriority Priority, DateTime? DueDate, TimeSpan? EstimatedDuration, int CategoryId, string? UpdatedBy);
public record UpdateTaskStatusRequest(TaskItemStatus Status, string UpdatedBy);
public record CreateCategoryRequest([Required] string Name, string? Description, string Color, string? CreatedBy);
public record CreateReminderRequest([Required] string Title, string? Description, DateTime ReminderDateTime, ReminderType Type, int UserId, int? TaskId, string? CreatedBy);
public record UpdateReminderRequest([Required] string Title, string? Description, DateTime ReminderDateTime, ReminderType Type, int? TaskId, bool IsActive, string? UpdatedBy);
public record StartTimerRequest(string StartedBy);
public record StopTimerRequest(string? Notes);
public record CompleteTaskRequest(bool IsCompleted);
public record GenerateReportRequest(int UserId, DateTime Date);
