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

public class AddTaskViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<AddTaskViewModel> _logger;
    private bool _isBusy;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private TaskPriority _selectedPriority = TaskPriority.Medium;
    private Category? _selectedCategory;
    private DateTime _dueDate = DateTime.Today.AddDays(1);
    private TimeSpan _dueTime = TimeSpan.FromHours(17);
    private string _estimatedDuration = "60";
    private bool _isImportant;

    public AddTaskViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        SaveCommand = new Command(async () => await SaveTaskAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        
        _ = LoadCategoriesAsync();
        InitializePriorityOptions();
    }

    public AddTaskViewModel(ApiService apiService, ILogger<AddTaskViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        SaveCommand = new Command(async () => await SaveTaskAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
        
        _ = LoadCategoriesAsync();
        InitializePriorityOptions();
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

    public TaskPriority SelectedPriority
    {
        get => _selectedPriority;
        set
        {
            _selectedPriority = value;
            OnPropertyChanged();
        }
    }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
        }
    }

    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            _dueDate = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan DueTime
    {
        get => _dueTime;
        set
        {
            _dueTime = value;
            OnPropertyChanged();
        }
    }

    public string EstimatedDuration
    {
        get => _estimatedDuration;
        set
        {
            _estimatedDuration = value;
            OnPropertyChanged();
        }
    }

    public bool IsImportant
    {
        get => _isImportant;
        set
        {
            _isImportant = value;
            OnPropertyChanged();
        }
    }

    public DateTime MinDate => DateTime.Today;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<string> PriorityOptions { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void InitializePriorityOptions()
    {
        PriorityOptions.Clear();
        foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
        {
            PriorityOptions.Add(priority.ToString());
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _apiService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Kategoriler yüklenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Kategoriler yüklenemedi", "Tamam");
        }
    }

    private async Task SaveTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Görev başlığı gereklidir", "Tamam");
            return;
        }

        IsBusy = true;

        try
        {
            var dueDateTime = DueDate.Date.Add(DueTime);
            var estimatedMinutes = int.TryParse(EstimatedDuration, out var minutes) ? minutes : 0;

            var newTask = new TaskItem
            {
                Title = Title,
                Description = Description,
                Priority = SelectedPriority,
                CategoryId = SelectedCategory?.Id ?? 1,
                DueDate = dueDateTime,
                EstimatedDuration = TimeSpan.FromMinutes(estimatedMinutes),
                Status = TaskItemStatus.NotStarted,
                CreatedAt = DateTime.Now,
                UserId = UserSessionService.Instance.CurrentUser?.Id ?? 1 // Kullanıcı oturumundan al
            };

            await _apiService.CreateTaskAsync(newTask);
            
            // Görev başarıyla eklendi mesajı gönder
            WeakReferenceMessenger.Default.Send(new TaskChangedMessage("TaskAdded"));
            
            await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Görev başarıyla eklendi", "Tamam");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görev kaydedilirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Görev kaydedilemedi", "Tamam");
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
