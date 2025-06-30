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

public class AddReminderViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<AddReminderViewModel> _logger;
    private bool _isBusy;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private DateTime _reminderDate = DateTime.Today.AddDays(1);
    private TimeSpan _reminderTime = TimeSpan.FromHours(9);
    private int _selectedTypeIndex = 0;
    private TaskItem? _selectedTask;

    public AddReminderViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        SaveCommand = new Command(async () => await SaveReminderAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        
        _ = LoadTasksAsync();
        InitializeTypeOptions();
    }

    public AddReminderViewModel(ApiService apiService, ILogger<AddReminderViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        SaveCommand = new Command(async () => await SaveReminderAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        
        _ = LoadTasksAsync();
        InitializeTypeOptions();
    }

    private bool CanSave()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(Title);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public DateTime ReminderDate
    {
        get => _reminderDate;
        set
        {
            _reminderDate = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan ReminderTime
    {
        get => _reminderTime;
        set
        {
            _reminderTime = value;
            OnPropertyChanged();
        }
    }

    public int SelectedTypeIndex
    {
        get => _selectedTypeIndex;
        set
        {
            _selectedTypeIndex = value;
            OnPropertyChanged();
        }
    }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            _selectedTask = value;
            OnPropertyChanged();
        }
    }

    public DateTime MinDate => DateTime.Today;

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<string> TypeOptions { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void InitializeTypeOptions()
    {
        TypeOptions.Clear();
        foreach (ReminderType type in Enum.GetValues<ReminderType>())
        {
            TypeOptions.Add(type.ToString());
        }
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            // Kullanıcı oturumu kontrol et
            var userSession = UserSessionService.Instance;
            if (userSession.CurrentUser == null)
            {
                return; // Kullanıcı oturumu yoksa görev yükleme
            }

            var tasks = await _apiService.GetUserTasksAsync(userSession.CurrentUser.Id);
            Tasks.Clear();
            foreach (var task in tasks)
            {
                Tasks.Add(task);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görevler yüklenirken hata oluştu");
            // Hata mesajını logla ama kullanıcıya gösterme
        }
    }

    private async Task SaveReminderAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcı başlığı gereklidir", "Tamam");
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
            var reminderDateTime = ReminderDate.Date.Add(ReminderTime);
            System.Diagnostics.Debug.WriteLine($"AddReminderViewModel: ReminderDate={ReminderDate:yyyy-MM-dd}, ReminderTime={ReminderTime}, Combined={reminderDateTime:yyyy-MM-dd HH:mm:ss}");
            
            var reminderType = (ReminderType)SelectedTypeIndex;

            var newReminder = new Reminder
            {
                Title = Title,
                Description = Description,
                ReminderDateTime = reminderDateTime,
                Type = reminderType,
                UserId = userSession.CurrentUser.Id,
                TaskId = SelectedTask?.Id,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = userSession.CurrentUser.Email
            };

            System.Diagnostics.Debug.WriteLine($"AddReminderViewModel: Yeni hatırlatıcı oluşturuluyor - DateTime={newReminder.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");

            await _apiService.CreateReminderAsync(newReminder);
            
            // Hatırlatıcı başarıyla eklendi mesajı gönder
            System.Diagnostics.Debug.WriteLine("ReminderAdded mesajı gönderiliyor...");
            WeakReferenceMessenger.Default.Send(new ReminderChangedMessage("ReminderAdded"));
            
            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Hatırlatıcı başarıyla eklendi", "Tamam");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı kaydedilirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcı kaydedilemedi", "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
