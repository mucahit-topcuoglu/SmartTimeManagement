using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.MAUI.Services;
using SmartTimeManagement.MAUI.Messages;

namespace SmartTimeManagement.MAUI.ViewModels;

public class RemindersViewModel : INotifyPropertyChanged, IRecipient<TaskChangedMessage>, IRecipient<ReminderChangedMessage>
{
    private readonly ApiService _apiService;
    private readonly ILogger<RemindersViewModel> _logger;
    private bool _isBusy;
    private int _selectedFilterIndex;

    public RemindersViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        InitializeCommands();
        InitializeFilterOptions();
        
        // Mesaj alıcısını kaydet
        WeakReferenceMessenger.Default.Register<TaskChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ReminderChangedMessage>(this);
        
        // User session değişikliklerini dinle
        UserSessionService.Instance.UserChanged += OnUserChanged;
        
        _ = LoadRemindersAsync();
    }

    public RemindersViewModel(ApiService apiService, ILogger<RemindersViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        InitializeCommands();
        InitializeFilterOptions();
        
        // Mesaj alıcısını kaydet
        WeakReferenceMessenger.Default.Register<TaskChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ReminderChangedMessage>(this);
        
        // User session değişikliklerini dinle
        UserSessionService.Instance.UserChanged += OnUserChanged;
        
        _ = LoadRemindersAsync();
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

    public int SelectedFilterIndex
    {
        get => _selectedFilterIndex;
        set
        {
            _selectedFilterIndex = value;
            OnPropertyChanged();
            FilterReminders();
        }
    }

    public bool IsEmpty => !Reminders.Any();

    public ObservableCollection<Reminder> Reminders { get; } = new();
    public ObservableCollection<Reminder> AllReminders { get; } = new();
    public ObservableCollection<string> FilterOptions { get; } = new();

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddReminderCommand { get; private set; } = null!;
    public ICommand EditReminderCommand { get; private set; } = null!;
    public ICommand DeleteReminderCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        RefreshCommand = new Command(async () => await LoadRemindersAsync());
        AddReminderCommand = new Command(async () => await AddReminderAsync());
        EditReminderCommand = new Command<Reminder>(async (reminder) => await EditReminderAsync(reminder));
        DeleteReminderCommand = new Command<Reminder>(async (reminder) => await DeleteReminderAsync(reminder));
    }

    private void InitializeFilterOptions()
    {
        FilterOptions.Clear();
        FilterOptions.Add("Tümü");
        FilterOptions.Add("Aktif");
        FilterOptions.Add("Pasif");
        FilterOptions.Add("Bugün");
        FilterOptions.Add("Bu Hafta");
    }

    private async Task LoadRemindersAsync()
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

            // Kullanıcıya özel hatırlatıcıları al
            var reminders = await _apiService.GetUserRemindersAsync(userSession.CurrentUser.Id);
            
            System.Diagnostics.Debug.WriteLine($"Kullanıcı {userSession.CurrentUser.Id} için API'den {reminders.Count} hatırlatıcı alındı");
            
            AllReminders.Clear();
            foreach (var reminder in reminders)
            {
                AllReminders.Add(reminder);
                System.Diagnostics.Debug.WriteLine($"Hatırlatıcı eklendi: {reminder.Title} - ID: {reminder.Id} - UserId: {reminder.UserId} - DateTime: {reminder.ReminderDateTime:yyyy-MM-dd HH:mm:ss}");
            }

            FilterReminders();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcılar yüklenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcılar yüklenemedi", "Tamam");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private void FilterReminders()
    {
        var filteredReminders = SelectedFilterIndex switch
        {
            1 => AllReminders.Where(r => r.IsActive),
            2 => AllReminders.Where(r => !r.IsActive),
            3 => AllReminders.Where(r => r.ReminderDateTime.Date == DateTime.Today),
            4 => AllReminders.Where(r => r.ReminderDateTime.Date >= DateTime.Today && 
                                       r.ReminderDateTime.Date <= DateTime.Today.AddDays(7)),
            _ => AllReminders
        };

        Reminders.Clear();
        foreach (var reminder in filteredReminders.OrderBy(r => r.ReminderDateTime))
        {
            Reminders.Add(reminder);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task AddReminderAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//reminders/add");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı ekleme sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task EditReminderAsync(Reminder reminder)
    {
        if (reminder == null) return;

        try
        {
            await Shell.Current.GoToAsync($"//reminders/edit?id={reminder.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı düzenleme sayfasına geçiş hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Sayfa açılamadı", "Tamam");
        }
    }

    private async Task DeleteReminderAsync(Reminder reminder)
    {
        if (reminder == null) return;

        var result = await Application.Current!.MainPage!.DisplayAlert(
            "Hatırlatıcı Sil", 
            $"'{reminder.Title}' hatırlatıcısını silmek istediğinize emin misiniz?", 
            "Evet", 
            "Hayır");

        if (!result) return;

        try
        {
            await _apiService.DeleteReminderAsync(reminder.Id);
            AllReminders.Remove(reminder);
            FilterReminders();
            
            // Hatırlatıcı silindi mesajı gönder
            WeakReferenceMessenger.Default.Send(new ReminderChangedMessage("ReminderDeleted"));
            
            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Hatırlatıcı silindi", "Tamam");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Hatırlatıcı silinirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Hatırlatıcı silinemedi", "Tamam");
        }
    }

    public void Receive(TaskChangedMessage message)
    {
        // Hatırlatıcı değiştiğinde listeyi yeniden yükle
        if (message.Value.Contains("Reminder"))
        {
            _ = LoadRemindersAsync();
        }
    }

    public void Receive(ReminderChangedMessage message)
    {
        // Hatırlatıcı değişikliklerini dinle
        System.Diagnostics.Debug.WriteLine($"ReminderChangedMessage alındı: {message.Value}");
        _ = LoadRemindersAsync();
    }

    private void OnUserChanged(object? sender, User? user)
    {
        if (user == null)
        {
            // Logout olduğunda tüm hatırlatıcıları temizle
            System.Diagnostics.Debug.WriteLine("RemindersViewModel: User logged out, clearing reminders");
            AllReminders.Clear();
            Reminders.Clear();
            OnPropertyChanged(nameof(IsEmpty));
        }
        else
        {
            // Yeni kullanıcı login oldu, hatırlatıcıları yeniden yükle
            System.Diagnostics.Debug.WriteLine($"RemindersViewModel: User logged in: {user.Id}, reloading reminders");
            _ = LoadRemindersAsync();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
