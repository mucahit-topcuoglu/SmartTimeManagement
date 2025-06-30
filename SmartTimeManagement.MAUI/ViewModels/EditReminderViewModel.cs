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

[QueryProperty(nameof(ReminderId), "id")]
public class EditReminderViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<EditReminderViewModel> _logger;
    private bool _isBusy;
    private int _reminderId;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private DateTime _reminderDate = DateTime.Today.AddDays(1);
    private TimeSpan _reminderTime = TimeSpan.FromHours(9);
    private int _selectedTypeIndex = 0;
    private TaskItem? _selectedTask;
    private bool _isActive = true;

    public EditReminderViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        SaveCommand = new Command(async () => await SaveReminderAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        DeleteCommand = new Command(async () => await DeleteReminderAsync());
        
        InitializeTypeOptions();
    }

    public EditReminderViewModel(ApiService apiService, ILogger<EditReminderViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        SaveCommand = new Command(async () => await SaveReminderAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        DeleteCommand = new Command(async () => await DeleteReminderAsync());
        
        InitializeTypeOptions();
    }

    private bool CanSave()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(Title);
    }

    public int ReminderId
    {
        get => _reminderId;
        set
        {
            _reminderId = value;
            OnPropertyChanged();
            if (_reminderId > 0)
            {
                _ = LoadReminderAsync();
            }
        }
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

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public DateTime MinDate => DateTime.Today;

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<string> TypeOptions { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }

    private void InitializeTypeOptions()
    {
        TypeOptions.Clear();
        foreach (ReminderType type in Enum.GetValues<ReminderType>())
        {
            TypeOptions.Add(type.ToString());
        }
    }

    private async Task LoadReminderAsync()
    {
        if (ReminderId <= 0) return;

        IsBusy = true;

        try
        {
            var reminder = await _apiService.GetReminderAsync(ReminderId);
            if (reminder != null)
            {
                Title = reminder.Title;
                Description = reminder.Description ?? string.Empty;
                ReminderDate = reminder.ReminderDateTime.Date;
                ReminderTime = reminder.ReminderDateTime.TimeOfDay;
                SelectedTypeIndex = (int)reminder.Type;
                IsActive = reminder.IsActive;

                // Görevleri yükle
                await LoadTasksAsync();

                // İlgili görevi seç
                if (reminder.TaskId.HasValue)
                {
                    SelectedTask = Tasks.FirstOrDefault(t => t.Id == reminder.TaskId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı yüklenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcı yüklenemedi", "Tamam");
        }
        finally
        {
            IsBusy = false;
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
            var reminderType = (ReminderType)SelectedTypeIndex;

            var updatedReminder = new Reminder
            {
                Id = ReminderId,
                Title = Title,
                Description = Description,
                ReminderDateTime = reminderDateTime,
                Type = reminderType,
                UserId = userSession.CurrentUser.Id,
                TaskId = SelectedTask?.Id,
                IsActive = IsActive,
                UpdatedAt = DateTime.Now,
                UpdatedBy = userSession.CurrentUser.Email
            };

            await _apiService.UpdateReminderAsync(updatedReminder);
            
            // Hatırlatıcı başarıyla güncellendi mesajı gönder
            WeakReferenceMessenger.Default.Send(new ReminderChangedMessage("ReminderUpdated"));
            
            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Hatırlatıcı başarıyla güncellendi", "Tamam");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı güncellenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcı güncellenemedi", "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteReminderAsync()
    {
        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Hatırlatıcı Sil", 
            $"'{Title}' hatırlatıcısını silmek istediğinize emin misiniz?", 
            "Evet", 
            "Hayır");

        if (!result) return;

        IsBusy = true;

        try
        {
            await _apiService.DeleteReminderAsync(ReminderId);
            
            // Hatırlatıcı başarıyla silindi mesajı gönder
            WeakReferenceMessenger.Default.Send(new ReminderChangedMessage("ReminderDeleted"));
            
            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Hatırlatıcı silindi", "Tamam");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı silinirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcı silinemedi", "Tamam");
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
