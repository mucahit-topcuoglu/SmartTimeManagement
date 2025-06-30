using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.MAUI.Services;

namespace SmartTimeManagement.MAUI.ViewModels;

public class ReportsViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<ReportsViewModel> _logger;
    private bool _isBusy;
    private DateTime _startDate = DateTime.Today.AddDays(-30);
    private DateTime _endDate = DateTime.Today;
    private bool _hasReportData;
    private int _totalTasks;
    private int _completedTasks;
    private int _inProgressTasks;
    private int _pendingTasks;
    private double _totalHours;
    private double _completionRate;

    public ReportsViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        InitializeCommands();
        _ = LoadRecentReportsAsync();
    }

    public ReportsViewModel(ApiService apiService, ILogger<ReportsViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        InitializeCommands();
        _ = LoadRecentReportsAsync();
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            _startDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            _endDate = value;
            OnPropertyChanged();
        }
    }

    public bool HasReportData
    {
        get => _hasReportData;
        set
        {
            _hasReportData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public int TotalTasks
    {
        get => _totalTasks;
        set
        {
            _totalTasks = value;
            OnPropertyChanged();
            CalculateCompletionRate();
        }
    }

    public int CompletedTasks
    {
        get => _completedTasks;
        set
        {
            _completedTasks = value;
            OnPropertyChanged();
            CalculateCompletionRate();
        }
    }

    public int InProgressTasks
    {
        get => _inProgressTasks;
        set
        {
            _inProgressTasks = value;
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

    public double TotalHours
    {
        get => _totalHours;
        set
        {
            _totalHours = value;
            OnPropertyChanged();
        }
    }

    public double CompletionRate
    {
        get => _completionRate;
        set
        {
            _completionRate = value;
            OnPropertyChanged();
        }
    }

    public bool IsEmpty => !HasReportData && !RecentReports.Any();

    public ObservableCollection<Report> RecentReports { get; } = new();
    public ObservableCollection<CategoryStatModel> CategoryStats { get; } = new();

    public ICommand GenerateReportCommand { get; private set; } = null!;
    public ICommand ViewReportCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        GenerateReportCommand = new Command(async () => await GenerateReportAsync());
        ViewReportCommand = new Command<Report>(async (report) => await ViewReportAsync(report));
    }

    private void CalculateCompletionRate()
    {
        CompletionRate = TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
    }

    private async Task GenerateReportAsync()
    {
        if (IsBusy) return;

        if (StartDate > EndDate)
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Başlangıç tarihi bitiş tarihinden sonra olamaz", "Tamam");
            return;
        }

        // Kullanıcı oturumu kontrol et
        var userSession = UserSessionService.Instance;
        if (userSession.CurrentUser == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Kullanıcı oturumu bulunamadı", "Tamam");
            return;
        }

        IsBusy = true;

        try
        {
            // Günlük rapor oluştur
            var dailyReportRequest = new
            {
                UserId = userSession.CurrentUser.Id,
                Date = EndDate
            };

            var dailyReport = await _apiService.GenerateDailyReportAsync(dailyReportRequest);
            
            // API'den rapor verileri çekme
            var tasks = await _apiService.GetTasksByDateRangeAsync(userSession.CurrentUser.Id, StartDate, EndDate);
            var timeLogs = await _apiService.GetTimeLogsByDateRangeAsync(userSession.CurrentUser.Id, StartDate, EndDate);

            // İstatistikleri hesapla
            TotalTasks = tasks.Count;
            CompletedTasks = tasks.Count(t => t.Status == Core.Enums.TaskItemStatus.Completed);
            InProgressTasks = tasks.Count(t => t.Status == Core.Enums.TaskItemStatus.InProgress);
            PendingTasks = tasks.Count(t => t.Status == Core.Enums.TaskItemStatus.NotStarted);
            TotalHours = timeLogs.Sum(tl => (double)tl.Duration.TotalMinutes) / 60.0; // dakikadan saate çevir

            // Kategori istatistikleri
            CategoryStats.Clear();
            var categoryGroups = tasks.Where(t => t.Category != null)
                                     .GroupBy(t => t.Category!.Name)
                                     .OrderByDescending(g => g.Count());

            foreach (var group in categoryGroups)
            {
                CategoryStats.Add(new CategoryStatModel
                {
                    CategoryName = group.Key,
                    TaskCount = group.Count()
                });
            }

            HasReportData = true;

            // Raporu veritabanına kaydet
            var report = new Report
            {
                Title = $"Üretkenlik Raporu ({StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy})",
                Type = Core.Enums.ReportType.Daily,
                ReportData = $"Toplam Görev: {TotalTasks}, Tamamlanan: {CompletedTasks}, Toplam Süre: {TotalHours:F1} saat",
                CreatedAt = DateTime.Now,
                UserId = 1 // Geçici - session'dan alınacak
            };

            await _apiService.CreateReportAsync(report);
            await LoadRecentReportsAsync();

            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Rapor oluşturuldu", "Tamam");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Rapor oluşturulurken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Rapor oluşturulamadı", "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadRecentReportsAsync()
    {
        try
        {
            var reports = await _apiService.GetRecentReportsAsync();
            
            RecentReports.Clear();
            foreach (var report in reports.Take(10)) // Son 10 rapor
            {
                RecentReports.Add(report);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Son raporlar yüklenirken hata oluştu");
        }
    }

    private async Task ViewReportAsync(Report report)
    {
        if (report == null) return;

        await Application.Current!.MainPage!.DisplayAlert(
            report.Title, 
            report.ReportData ?? "Rapor verisi bulunamadı", 
            "Tamam");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class CategoryStatModel
{
    public string CategoryName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}
