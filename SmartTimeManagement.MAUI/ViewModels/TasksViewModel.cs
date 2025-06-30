using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;
using SmartTimeManagement.MAUI.Services;
using SmartTimeManagement.MAUI.Messages;

namespace SmartTimeManagement.MAUI.ViewModels;

public class TasksViewModel : INotifyPropertyChanged, IRecipient<TaskChangedMessage>, IDisposable
{
    private readonly ApiService _apiService;
    private readonly ILogger<TasksViewModel> _logger;
    private bool _isBusy;
    private int _selectedFilterIndex;
    private System.Timers.Timer? _uiUpdateTimer;
    private Dictionary<int, bool> _runningTimers = new Dictionary<int, bool>();
    private Dictionary<int, DateTime> _timerStartTimes = new Dictionary<int, DateTime>();
    private Dictionary<int, TimeSpan> _baseActualDuration = new Dictionary<int, TimeSpan>();

    public TasksViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        InitializeCommands();
        InitializeFilterOptions();
        
        // Mesaj alıcısını kaydet
        WeakReferenceMessenger.Default.Register(this);
        
        // User session değişikliklerini dinle
        UserSessionService.Instance.UserChanged += OnUserChanged;
        
        // UI güncelleme timer'ı başlat
        StartUIUpdateTimer();
        
        _ = LoadTasksAsync();
    }

    public TasksViewModel(ApiService apiService, ILogger<TasksViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        InitializeCommands();
        InitializeFilterOptions();
        
        // Mesaj alıcısını kaydet
        WeakReferenceMessenger.Default.Register(this);
        
        // User session değişikliklerini dinle
        UserSessionService.Instance.UserChanged += OnUserChanged;
        
        // UI güncelleme timer'ı başlat
        StartUIUpdateTimer();
        
        _ = LoadTasksAsync();
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)RefreshCommand).ChangeCanExecute();
        }
    }

    public int SelectedFilterIndex
    {
        get => _selectedFilterIndex;
        set
        {
            _selectedFilterIndex = value;
            OnPropertyChanged();
            FilterTasks();
        }
    }

    public bool IsTimerRunning(int taskId)
    {
        // Check both local state and API state
        var hasLocalTimer = _runningTimers.ContainsKey(taskId) && _runningTimers[taskId];
        var task = AllTasks.FirstOrDefault(t => t.Id == taskId);
        var hasAPITimer = task?.Status == TaskItemStatus.InProgress && task?.IsTimerRunning == true;
        
        return hasLocalTimer || hasAPITimer;
    }

    public string GetTimerText(int taskId)
    {
        if (IsTimerRunning(taskId))
        {
            return "Durdur";
        }
        return "Başlat";
    }

    public bool IsEmpty => !Tasks.Any();

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> AllTasks { get; } = new();
    public ObservableCollection<string> FilterOptions { get; } = new();

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddTaskCommand { get; private set; } = null!;
    public ICommand EditTaskCommand { get; private set; } = null!;
    public ICommand DeleteTaskCommand { get; private set; } = null!;
    public ICommand StartTimerCommand { get; private set; } = null!;
    public ICommand StopTimerCommand { get; private set; } = null!;
    public ICommand ToggleTimerCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        RefreshCommand = new Command(async () => await LoadTasksAsync(), () => !IsBusy);
        AddTaskCommand = new Command(async () => await AddTaskAsync());
        EditTaskCommand = new Command<TaskItem>(async (task) => await EditTaskAsync(task));
        DeleteTaskCommand = new Command<TaskItem>(async (task) => await DeleteTaskAsync(task));
        StartTimerCommand = new Command<TaskItem>(async (task) => await StartTimerAsync(task));
        StopTimerCommand = new Command<TaskItem>(async (task) => await StopTimerAsync(task));
        ToggleTimerCommand = new Command<TaskItem>(async (task) => await ToggleTimerAsync(task));
    }

    private void InitializeFilterOptions()
    {
        FilterOptions.Clear();
        FilterOptions.Add("Tümü");
        FilterOptions.Add("Bekleyen");
        FilterOptions.Add("Devam Eden");
        FilterOptions.Add("Tamamlanan");
        FilterOptions.Add("Yüksek Öncelik");
        FilterOptions.Add("Bugün");
    }

    private async Task LoadTasksAsync()
    {
        if (IsBusy) return;

        IsBusy = true;

        try
        {
            // Kullanıcı oturumunu kontrol et
            var userSession = UserSessionService.Instance;
            if (userSession.CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("Kullanıcı oturumu bulunamadı");
                return;
            }

            // Kullanıcıya özel görevleri al
            var tasks = await _apiService.GetUserTasksAsync(userSession.CurrentUser.Id);
            
            System.Diagnostics.Debug.WriteLine($"Kullanıcı {userSession.CurrentUser.Id} için API'den {tasks.Count} görev alındı");
            
            AllTasks.Clear();
            foreach (var task in tasks)
            {
                AllTasks.Add(task);
                System.Diagnostics.Debug.WriteLine($"Görev eklendi: {task.Title} - ID: {task.Id} - UserId: {task.UserId}");
            }

            FilterTasks();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görevler yüklenirken hata oluştu");
            System.Diagnostics.Debug.WriteLine($"LoadTasks error: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Hata", $"Görevler yüklenemedi: {ex.Message}", "Tamam");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private void FilterTasks()
    {
        var filteredTasks = SelectedFilterIndex switch
        {
            1 => AllTasks.Where(t => t.Status == TaskItemStatus.NotStarted),
            2 => AllTasks.Where(t => t.Status == TaskItemStatus.InProgress),
            3 => AllTasks.Where(t => t.Status == TaskItemStatus.Completed),
            4 => AllTasks.Where(t => t.Priority == TaskPriority.High),
            5 => AllTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today),
            _ => AllTasks
        };

        Tasks.Clear();
        foreach (var task in filteredTasks.OrderBy(t => t.DueDate))
        {
            Tasks.Add(task);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task AddTaskAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//tasks/add");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görev ekleme sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task EditTaskAsync(TaskItem task)
    {
        if (task == null) return;

        try
        {
            await Shell.Current.GoToAsync($"//tasks/edit?id={task.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görev düzenleme sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task DeleteTaskAsync(TaskItem task)
    {
        if (task == null) return;

        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Görev Sil", 
            $"'{task.Title}' görevini silmek istediğinize emin misiniz?", 
            "Evet", 
            "Hayır");

        if (!result) return;

        try
        {
            await _apiService.DeleteTaskAsync(task.Id);
            AllTasks.Remove(task);
            FilterTasks();
            
            // Görev silindi mesajı gönder
            WeakReferenceMessenger.Default.Send(new TaskChangedMessage("TaskDeleted"));
            
            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Görev silindi", "Tamam");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görev silinirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Görev silinemedi", "Tamam");
        }
    }

    private async Task StartTimerAsync(TaskItem task)
    {
        if (task == null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"StartTimerAsync başladı: TaskId={task.Id}");
            var updatedTask = await _apiService.StartTaskTimerAsync(task.Id);
            if (updatedTask != null)
            {
                System.Diagnostics.Debug.WriteLine($"API'den güncellenen task alındı: Status={updatedTask.Status}, IsTimerRunning={updatedTask.IsTimerRunning}");
                
                // Listedeki görevi güncelle
                var index = AllTasks.ToList().FindIndex(t => t.Id == task.Id);
                if (index >= 0)
                {
                    AllTasks[index] = updatedTask;
                    FilterTasks();
                    System.Diagnostics.Debug.WriteLine($"Task listede güncellendi: index={index}");
                }
                
                // Timer durumunu güncelle
                UpdateTimerState(task, true);
                
                await Application.Current!.MainPage!.DisplayAlert("Timer", $"'{task.Title}' görevi için timer başlatıldı ve durum 'Devam Ediyor' olarak güncellendi", "Tamam");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("API'den null task döndü");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Timer başlatılırken hata oluştu");
            System.Diagnostics.Debug.WriteLine($"StartTimerAsync hata: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Timer başlatılamadı", "Tamam");
        }
    }

    private async Task StopTimerAsync(TaskItem task)
    {
        if (task == null) return;

        try
        {
            var updatedTask = await _apiService.StopTaskTimerAsync(task.Id);
            if (updatedTask != null)
            {
                // Timer durumunu güncelle
                UpdateTimerState(task, false);
                
                // Kullanıcıya seçenek sun
                var action = await Application.Current!.MainPage!.DisplayActionSheet(
                    $"'{task.Title}' görevinin timer'ı durduruldu. Görev durumu ne olsun?",
                    "İptal",
                    null,
                    "Tamamlandı",
                    "Tamamlanamadı (Beklemede)"
                );

                bool? isCompleted = action switch
                {
                    "Tamamlandı" => true,
                    "Tamamlanamadı (Beklemede)" => false,
                    _ => null
                };

                if (isCompleted.HasValue)
                {
                    var finalTask = await _apiService.CompleteTaskAsync(task.Id, isCompleted.Value);
                    if (finalTask != null)
                    {
                        // Listedeki görevi güncelle
                        var index = AllTasks.ToList().FindIndex(t => t.Id == task.Id);
                        if (index >= 0)
                        {
                            AllTasks[index] = finalTask;
                            FilterTasks();
                        }
                    }
                }
                else
                {
                    // İptal edildi, sadece timer'ı durdur
                    var index = AllTasks.ToList().FindIndex(t => t.Id == task.Id);
                    if (index >= 0)
                    {
                        AllTasks[index] = updatedTask;
                        FilterTasks();
                    }
                }
                
                var elapsedTime = updatedTask.ActualDuration?.ToString(@"hh\:mm\:ss") ?? "00:00:00";
                await Application.Current!.MainPage!.DisplayAlert("Timer", $"Toplam çalışma süresi: {elapsedTime}", "Tamam");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Timer durdurulurken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Timer durdurulamadı", "Tamam");
        }
    }

    private async Task ToggleTimerAsync(TaskItem task)
    {
        if (task == null)
        {
            System.Diagnostics.Debug.WriteLine("ToggleTimerAsync: task is null");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"ToggleTimerAsync: TaskId={task.Id}, Title='{task.Title}', IsTimerRunning={IsTimerRunning(task.Id)}");

        // Geçici olarak ID kontrolünü kaldırdık - debug için
        // if (task.Id <= 0)
        // {
        //     System.Diagnostics.Debug.WriteLine($"ToggleTimerAsync: Geçersiz task ID: {task.Id}");
        //     await Application.Current!.MainPage!.DisplayAlert("Hata", "Geçersiz görev ID", "Tamam");
        //     return;
        // }

        if (IsTimerRunning(task.Id))
        {
            System.Diagnostics.Debug.WriteLine("Timer durduruluyor...");
            await StopTimerAsync(task);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Timer başlatılıyor...");
            await StartTimerAsync(task);
        }
    }

    private void StartUIUpdateTimer()
    {
        _uiUpdateTimer = new System.Timers.Timer(1000); // Her saniye güncelle
        _uiUpdateTimer.Elapsed += (sender, e) => UpdateTimerDisplays();
        _uiUpdateTimer.AutoReset = true;
        _uiUpdateTimer.Start();
    }

    private void UpdateTimerDisplays()
    {
        // Ana thread'de UI güncellemelerini yap
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool hasChanges = false;
            
            // Çalışan timer'ları güncelle
            foreach (var task in Tasks.Where(t => t.Status == TaskItemStatus.InProgress && t.IsTimerRunning))
            {
                if (_timerStartTimes.ContainsKey(task.Id) && _baseActualDuration.ContainsKey(task.Id))
                {
                    var currentSessionElapsed = DateTime.Now - _timerStartTimes[task.Id];
                    var baseActualDuration = _baseActualDuration[task.Id];
                    
                    // Toplam süreyi güncelle (base süre + şu anki oturum süresi)
                    var newDuration = baseActualDuration + currentSessionElapsed;
                    
                    if (task.ActualDuration != newDuration)
                    {
                        task.ActualDuration = newDuration;
                        hasChanges = true;
                    }
                }
            }
            
            // Sadece değişiklik varsa UI güncellemesi yap
            if (hasChanges)
            {
                OnPropertyChanged(nameof(Tasks));
            }
        });
    }

    public void Receive(TaskChangedMessage message)
    {
        // Görev değiştiğinde listeyi yeniden yükle
        _ = LoadTasksAsync();
    }

    private void UpdateTimerState(TaskItem task, bool isRunning)
    {
        // Timer durumunu güncelle
        _runningTimers[task.Id] = isRunning;
        
        // TaskItem'daki IsTimerRunning property'sini güncelle
        task.IsTimerRunning = isRunning;
        
        if (isRunning)
        {
            _timerStartTimes[task.Id] = DateTime.Now;
            task.TimerStartedAt = DateTime.Now;
            
            // Timer başlatıldığında mevcut ActualDuration'ı base olarak kaydet
            if (!_baseActualDuration.ContainsKey(task.Id))
            {
                _baseActualDuration[task.Id] = task.ActualDuration ?? TimeSpan.Zero;
            }
        }
        else
        {
            _timerStartTimes.Remove(task.Id);
            task.TimerStartedAt = null;
            _baseActualDuration.Remove(task.Id);
        }
        
        OnPropertyChanged(nameof(Tasks)); // UI güncellemesi için
    }

    private void OnUserChanged(object? sender, User? user)
    {
        if (user == null)
        {
            // Logout olduğunda tüm görevleri temizle
            System.Diagnostics.Debug.WriteLine("TasksViewModel: User logged out, clearing tasks");
            AllTasks.Clear();
            Tasks.Clear();
            OnPropertyChanged(nameof(IsEmpty));
            
            // Timer'ları temizle
            _runningTimers.Clear();
            _timerStartTimes.Clear();
            _baseActualDuration.Clear();
        }
        else
        {
            // Yeni kullanıcı login oldu, görevleri yeniden yükle
            System.Diagnostics.Debug.WriteLine($"TasksViewModel: User logged in: {user.Id}, reloading tasks");
            _ = LoadTasksAsync();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        // Event'i unsubscribe et
        UserSessionService.Instance.UserChanged -= OnUserChanged;
        
        _uiUpdateTimer?.Stop();
        _uiUpdateTimer?.Dispose();
        _runningTimers.Clear();
        _timerStartTimes.Clear();
        _baseActualDuration.Clear();
        WeakReferenceMessenger.Default.Unregister<TaskChangedMessage>(this);
    }
}
