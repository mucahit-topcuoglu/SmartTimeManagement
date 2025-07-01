using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;
using SmartTimeManagement.MAUI.Services;

namespace SmartTimeManagement.MAUI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<MainViewModel> _logger;
    private bool _isLoading;
    private string _welcomeMessage = "Hoş Geldiniz!";
    private int _totalTasks;
    private int _completedTasks;
    private int _pendingTasks;

    public MainViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        InitializeCommands();
        UpdateWelcomeMessage();
        _ = LoadDataAsync();
    }

    public MainViewModel(ApiService apiService, ILogger<MainViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        InitializeCommands();
        UpdateWelcomeMessage();
        _ = LoadDataAsync();
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            ((Command)RefreshCommand).ChangeCanExecute();
        }
    }

    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            _welcomeMessage = value;
            OnPropertyChanged();
        }
    }



    public int TotalTasks
    {
        get => _totalTasks;
        set
        {
            _totalTasks = value;
            OnPropertyChanged();
        }
    }

    public int CompletedTasks
    {
        get => _completedTasks;
        set
        {
            _completedTasks = value;
            OnPropertyChanged();
        }
    }

    public int PendingTasks
    {
        get => _pendingTasks;
        set
        {
            _pendingTasks = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<TaskItem> TodaysTasks { get; } = new();
    public ObservableCollection<ActivityModel> RecentActivities { get; } = new();

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand LogoutCommand { get; private set; } = null!;
    public ICommand NewTaskCommand { get; private set; } = null!;
    public ICommand NewReminderCommand { get; private set; } = null!;
    public ICommand ViewTasksCommand { get; private set; } = null!;
    public ICommand CreateReportCommand { get; private set; } = null!;
    public ICommand ChangePasswordCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        RefreshCommand = new Command(async () => await LoadDataAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        NewTaskCommand = new Command(async () => await NavigateToNewTaskAsync());
        NewReminderCommand = new Command(async () => await NavigateToNewReminderAsync());
        ViewTasksCommand = new Command(async () => await NavigateToTasksAsync());
        CreateReportCommand = new Command(async () => await NavigateToReportsAsync());
        ChangePasswordCommand = new Command(async () => await NavigateToChangePasswordAsync());
    }

    private void UpdateWelcomeMessage()
    {
        var hour = DateTime.Now.Hour;
        var greeting = hour switch
        {
            < 12 => "Günaydın",
            < 18 => "İyi günler",
            _ => "İyi akşamlar"
        };
        
        WelcomeMessage = $"{greeting}! Üretken bir gün geçirmeniz dileğiyle.";
    }

    private async Task LoadDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            // Kullanıcı oturumunu kontrol et
            var userSession = UserSessionService.Instance;
            if (userSession.CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: Kullanıcı oturumu bulunamadı");
                return;
            }

            // Kullanıcıya özel görevleri yükle
            var tasks = await _apiService.GetUserTasksAsync(userSession.CurrentUser.Id);
            
            // İstatistikleri hesapla
            TotalTasks = tasks.Count;
            CompletedTasks = tasks.Count(t => t.Status == TaskItemStatus.Completed);
            PendingTasks = tasks.Count(t => t.Status == TaskItemStatus.NotStarted);

            // Bugünün görevlerini filtrele
            var todaysTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today).ToList();
            
            TodaysTasks.Clear();
            foreach (var task in todaysTasks.Take(5)) // En fazla 5 göstər
            {
                TodaysTasks.Add(task);
            }

            // Son aktiviteleri oluştur
            RecentActivities.Clear();
            RecentActivities.Add(new ActivityModel { Description = "Uygulama başlatıldı", DateTime = DateTime.Now });
            
            if (tasks.Any())
            {
                var recentTask = tasks.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
                if (recentTask != null)
                {
                    RecentActivities.Add(new ActivityModel 
                    { 
                        Description = $"'{recentTask.Title}' görevi oluşturuldu", 
                        DateTime = recentTask.CreatedAt 
                    });
                }
            }

            UpdateWelcomeMessage();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ana sayfa verileri yüklenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Veriler yüklenemedi", "Tamam");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LogoutAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Çıkış", 
            "Uygulamadan çıkmak istediğinize emin misiniz?", 
            "Evet", 
            "Hayır");

        if (result)
        {
            try
            {
                // Önce verileri temizle
                TotalTasks = 0;
                CompletedTasks = 0;
                PendingTasks = 0;
                TodaysTasks.Clear();
                RecentActivities.Clear();
                
                // Sonra session'ı temizle
                UserSessionService.Instance.Logout();
                
                // Login sayfasına yönlendir
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Çıkış yapılırken hata oluştu");
            }
        }
    }

    private async Task NavigateToNewTaskAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//tasks/add");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Yeni görev sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task NavigateToNewReminderAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//reminders/add");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Yeni hatırlatıcı sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task NavigateToTasksAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//tasks");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görevler sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task NavigateToReportsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//reports");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Raporlar sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task CreateNewTaskAsync()
    {
        await NavigateToNewTaskAsync();
    }

    private async Task CreateNewReminderAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("reminders/add");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Yeni hatırlatıcı sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task ViewTasksAsync()
    {
        await NavigateToTasksAsync();
    }

    private async Task CreateReportAsync()
    {
        await NavigateToReportsAsync();
    }

    private async Task NavigateToChangePasswordAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//change-password");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Şifre değiştirme sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ActivityModel
{
    public string Description { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
}
