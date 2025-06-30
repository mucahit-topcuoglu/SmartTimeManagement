using System.Text;
using System.Text.Json;
using SmartTimeManagement.Core.Entities;

namespace SmartTimeManagement.MAUI.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService()
    {
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _baseUrl = "http://localhost:5001/api"; // API portu 5001'e değiştirildi
        
        // JSON serialization seçenekleri
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    // User Methods
    public async Task<User?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new { Email = email, Password = password };
            var response = await PostAsync("/users/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Login API Response: {content}");
                
                var user = JsonSerializer.Deserialize<User>(content, _jsonOptions);
                
                System.Diagnostics.Debug.WriteLine($"Deserialized User: Id={user?.Id}, Email={user?.Email}, FirstName={user?.FirstName}");
                
                return user;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            // Log error for debugging
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            throw new Exception($"Giriş hatası: {ex.Message}");
        }
    }

    public async Task<bool> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"RegisterAsync called with: {firstName}, {lastName}, {email}");
            
            var request = new { FirstName = firstName, LastName = lastName, Email = email, Password = password };
            
            System.Diagnostics.Debug.WriteLine($"Making POST request to /users/register");
            var response = await PostAsync("/users/register", request);
            
            System.Diagnostics.Debug.WriteLine($"Register response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Register error response: {errorContent}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RegisterAsync Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"RegisterAsync StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"RegisterAsync InnerException: {ex.InnerException.Message}");
            }
            
            throw; // Re-throw to let ViewModel handle it
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var request = new { CurrentPassword = currentPassword, NewPassword = newPassword };
            var response = await PostAsync($"/users/{userId}/change-password", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChangePassword error: {ex.Message}");
            throw new Exception($"Şifre değiştirme hatası: {ex.Message}");
        }
    }

    // Task Methods
    public async Task<List<TaskItem>> GetUserTasksAsync(int userId)
    {
        System.Diagnostics.Debug.WriteLine($"MAUI: GetUserTasksAsync çağrıldı - userId: {userId}");
        var response = await GetAsync($"/tasks/user/{userId}");
        System.Diagnostics.Debug.WriteLine($"MAUI: API response status: {response.StatusCode}");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"MAUI: API response content: {content}");
            
            var tasks = JsonSerializer.Deserialize<List<TaskItem>>(content, _jsonOptions) ?? new List<TaskItem>();
            
            System.Diagnostics.Debug.WriteLine($"MAUI: Deserialize edildi - {tasks.Count} görev");
            foreach (var task in tasks.Take(3))
            {
                System.Diagnostics.Debug.WriteLine($"MAUI: Task ID={task.Id}, Title='{task.Title}', UserId={task.UserId}");
            }
            
            return tasks;
        }
        return new List<TaskItem>();
    }

    public async Task<List<TaskItem>> GetTasksAsync()
    {
        try
        {
            // Kullanıcı oturumunu kontrol et
            var userSession = UserSessionService.Instance;
            if (userSession.CurrentUser == null)
            {
                return new List<TaskItem>();
            }

            var response = await GetAsync($"/tasks/user/{userSession.CurrentUser.Id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TaskItem>>(content, _jsonOptions) ?? new List<TaskItem>();
            }
            return new List<TaskItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTasks error: {ex.Message}");
            throw new Exception($"Görevler yüklenirken hata: {ex.Message}");
        }
    }

    public async Task<TaskItem?> CreateTaskAsync(TaskItem task)
    {
        try
        {
            var response = await PostAsync("/tasks", task);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TaskItem>(content, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateTask error: {ex.Message}");
            throw new Exception($"Görev oluşturulurken hata: {ex.Message}");
        }
    }

    public async Task<TaskItem?> UpdateTaskAsync(TaskItem task)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MAUI: Görev güncelleniyor - ID: {task.Id}, Title: {task.Title}");
            
            // API'nin beklediği formatta request oluştur
            var updateRequest = new
            {
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                DueDate = task.DueDate,
                EstimatedDuration = task.EstimatedDuration,
                CategoryId = task.CategoryId,
                UpdatedBy = task.UpdatedBy
            };
            
            var response = await PutAsync($"/tasks/{task.Id}", updateRequest);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"MAUI: Görev başarıyla güncellendi - Response: {content}");
                return JsonSerializer.Deserialize<TaskItem>(content, _jsonOptions);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"MAUI: Görev güncelleme hatası - Status: {response.StatusCode}, Content: {errorContent}");
                throw new Exception($"Görev güncellenemedi: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MAUI: UpdateTask exception: {ex.Message}");
            throw new Exception($"Görev güncellenirken hata: {ex.Message}");
        }
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        try
        {
            var response = await DeleteAsync($"/tasks/{taskId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask error: {ex.Message}");
            throw new Exception($"Görev silinirken hata: {ex.Message}");
        }
    }

    public async Task<List<TaskItem>> GetTasksDueTodayAsync(int userId)
    {
        var response = await GetAsync($"/tasks/user/{userId}/due-today");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TaskItem>>(content) ?? new List<TaskItem>();
        }
        return new List<TaskItem>();
    }

    public async Task<List<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var response = await GetAsync($"/tasks/date-range?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<TaskItem>>(content, options) ?? new List<TaskItem>();
            }
            return new List<TaskItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTasksByDateRange error: {ex.Message}");
            throw new Exception($"Tarih aralığındaki görevler yüklenirken hata: {ex.Message}");
        }
    }

    public async Task<List<TaskItem>> GetTasksByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var response = await GetAsync($"/tasks/user/{userId}/date-range?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<TaskItem>>(content, options) ?? new List<TaskItem>();
            }
            return new List<TaskItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTasksByDateRange (user-specific) error: {ex.Message}");
            throw new Exception($"Kullanıcıya özel tarih aralığındaki görevler yüklenirken hata: {ex.Message}");
        }
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id)
    {
        try
        {
            var response = await GetAsync($"/tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<TaskItem>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskById error: {ex.Message}");
            throw new Exception($"Görev yüklenirken hata: {ex.Message}");
        }
    }

    // Category Methods
    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            var response = await GetAsync("/categories");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<Category>>(content, options) ?? new List<Category>();
            }
            return new List<Category>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetCategories error: {ex.Message}");
            throw new Exception($"Kategoriler yüklenirken hata: {ex.Message}");
        }
    }

    // Reminder Methods
    public async Task<List<Reminder>> GetUserRemindersAsync(int userId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetUserRemindersAsync: UserId={userId}");
            
            var response = await GetAsync($"/reminders/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"GetUserRemindersAsync: API Response = {content}");
                
                var reminders = JsonSerializer.Deserialize<List<Reminder>>(content, _jsonOptions) ?? new List<Reminder>();
                
                System.Diagnostics.Debug.WriteLine($"GetUserRemindersAsync: Deserialized {reminders.Count} reminders");
                foreach (var reminder in reminders.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"GetUserRemindersAsync: Reminder {reminder.Id} - DateTime={reminder.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
                }
                
                return reminders;
            }
            return new List<Reminder>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserRemindersAsync error: {ex.Message}");
            return new List<Reminder>();
        }
    }

    public async Task<List<Reminder>> GetRemindersAsync()
    {
        try
        {
            // Kullanıcı oturumunu kontrol et
            var userSession = UserSessionService.Instance;
            if (userSession.CurrentUser == null)
            {
                return new List<Reminder>();
            }

            var response = await GetAsync($"/reminders/user/{userSession.CurrentUser.Id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<Reminder>>(content, options) ?? new List<Reminder>();
            }
            return new List<Reminder>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetReminders error: {ex.Message}");
            throw new Exception($"Hatırlatıcılar yüklenirken hata: {ex.Message}");
        }
    }

    public async Task<Reminder> CreateReminderAsync(Reminder reminder)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CreateReminderAsync: Gönderilen ReminderDateTime = {reminder.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
            
            var response = await PostAsync("/reminders", reminder);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CreateReminderAsync: API Response = {content}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<Reminder>(content, options) ?? reminder;
                
                System.Diagnostics.Debug.WriteLine($"CreateReminderAsync: Deserialized ReminderDateTime = {result.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
                
                return result;
            }
            throw new Exception("Hatırlatıcı oluşturulamadı");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateReminder error: {ex.Message}");
            throw new Exception($"Hatırlatıcı oluşturulurken hata: {ex.Message}");
        }
    }

    public async Task<bool> DeleteReminderAsync(int id)
    {
        try
        {
            var response = await DeleteAsync($"/reminders/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteReminder error: {ex.Message}");
            throw new Exception($"Hatırlatıcı silinirken hata: {ex.Message}");
        }
    }

    public async Task<Reminder?> GetReminderAsync(int id)
    {
        try
        {
            var response = await GetAsync($"/reminders/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<Reminder>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetReminderAsync error: {ex.Message}");
            throw new Exception($"Hatırlatıcı getirme hatası: {ex.Message}");
        }
    }

    public async Task<Reminder> UpdateReminderAsync(Reminder reminder)
    {
        try
        {
            var response = await PutAsync($"/reminders/{reminder.Id}", reminder);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<Reminder>(content, options) ?? reminder;
            }
            throw new Exception("Hatırlatıcı güncellenemedi");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateReminderAsync error: {ex.Message}");
            throw new Exception($"Hatırlatıcı güncelleme hatası: {ex.Message}");
        }
    }

    // Report Methods
    public async Task<List<Report>> GetRecentReportsAsync()
    {
        try
        {
            var response = await GetAsync("/reports/recent");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<Report>>(content, options) ?? new List<Report>();
            }
            return new List<Report>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetRecentReports error: {ex.Message}");
            throw new Exception($"Son raporlar yüklenirken hata: {ex.Message}");
        }
    }

    public async Task<Report?> CreateReportAsync(Report report)
    {
        try
        {
            var response = await PostAsync("/reports", report);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<Report>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateReport error: {ex.Message}");
            throw new Exception($"Rapor oluşturulurken hata: {ex.Message}");
        }
    }

    // TaskTimeLog Methods
    public async Task<List<TaskTimeLog>> GetTimeLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var response = await GetAsync($"/timelogs/date-range?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<TaskTimeLog>>(content, options) ?? new List<TaskTimeLog>();
            }
            return new List<TaskTimeLog>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTimeLogsByDateRange error: {ex.Message}");
            throw new Exception($"Zaman kayıtları yüklenirken hata: {ex.Message}");
        }
    }

    public async Task<List<TaskTimeLog>> GetTimeLogsByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var response = await GetAsync($"/timelogs/date-range?userId={userId}&start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<TaskTimeLog>>(content, options) ?? new List<TaskTimeLog>();
            }
            return new List<TaskTimeLog>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTimeLogsByDateRange (user-specific) error: {ex.Message}");
            throw new Exception($"Kullanıcıya özel zaman kayıtları yüklenirken hata: {ex.Message}");
        }
    }

    // Timer Methods
    public async Task<TaskItem?> StartTaskTimerAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MAUI: API çağrısı yapılıyor - /tasks/{taskId}/timer/start");
            var response = await PostAsync($"/tasks/{taskId}/timer/start", new { });
            System.Diagnostics.Debug.WriteLine($"MAUI: API response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"MAUI: API response content: {content}");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<TaskItem>(content, options);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"MAUI: API error response: {errorContent}");
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartTaskTimer error: {ex.Message}");
            throw new Exception($"Timer başlatılırken hata: {ex.Message}");
        }
    }

    public async Task<TaskItem?> StopTaskTimerAsync(int taskId)
    {
        try
        {
            var response = await PostAsync($"/tasks/{taskId}/timer/stop", new { });
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<TaskItem>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StopTaskTimer error: {ex.Message}");
            throw new Exception($"Timer durdurulurken hata: {ex.Message}");
        }
    }

    public async Task<TaskItem?> CompleteTaskAsync(int taskId, bool isCompleted)
    {
        try
        {
            var request = new { IsCompleted = isCompleted };
            var response = await PostAsync($"/tasks/{taskId}/complete", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<TaskItem>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CompleteTask error: {ex.Message}");
            throw new Exception($"Görev tamamlanırken hata: {ex.Message}");
        }
    }

    // Helper Methods
    private async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
    }

    private async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"PostAsync called with endpoint: {_baseUrl}{endpoint}");
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(data, options);
            System.Diagnostics.Debug.WriteLine($"PostAsync JSON payload: {json}");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            System.Diagnostics.Debug.WriteLine($"Making HTTP POST request...");
            var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
            
            System.Diagnostics.Debug.WriteLine($"PostAsync response received with status: {response.StatusCode}");
            
            return response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PostAsync Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"PostAsync StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"PostAsync InnerException: {ex.InnerException.Message}");
            }
            
            throw;
        }
    }

    private async Task<HttpResponseMessage> PutAsync(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PutAsync($"{_baseUrl}{endpoint}", content);
    }

    private async Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        return await _httpClient.DeleteAsync($"{_baseUrl}{endpoint}");
    }

    // Report Methods
    public async Task<Report?> GenerateDailyReportAsync(object request)
    {
        try
        {
            var response = await PostAsync("/reports/daily", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<Report>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GenerateDailyReport error: {ex.Message}");
            throw new Exception($"Günlük rapor oluşturma hatası: {ex.Message}");
        }
    }

    public async Task<Report?> GenerateWeeklyReportAsync(object request)
    {
        try
        {
            var response = await PostAsync("/reports/weekly", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<Report>(content, options);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GenerateWeeklyReport error: {ex.Message}");
            throw new Exception($"Haftalık rapor oluşturma hatası: {ex.Message}");
        }
    }

    public async Task<List<Report>> GetUserReportsAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/reports/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<Report>>(content, options) ?? new List<Report>();
            }
            return new List<Report>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserReports error: {ex.Message}");
            throw new Exception($"Kullanıcı raporları alma hatası: {ex.Message}");
        }
    }
}
