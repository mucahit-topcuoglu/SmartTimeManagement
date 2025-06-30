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

[QueryProperty(nameof(TaskId), "id")]
public class EditTaskViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<EditTaskViewModel> _logger;
    private bool _isBusy;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private int _selectedPriorityIndex = 1;
    private int _selectedStatusIndex = 0;
    private Category? _selectedCategory;
    private DateTime _dueDate = DateTime.Today.AddDays(1);
    private TimeSpan _dueTime = TimeSpan.FromHours(17);
    private string _estimatedDuration = "60";
    private int _taskId;
    private TaskItem? _currentTask;

    public EditTaskViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        UpdateCommand = new Command(async () => await UpdateTaskAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(Title));
        CancelCommand = new Command(async () => await CancelAsync());
        
        InitializePriorityOptions();
        InitializeStatusOptions();
        _ = LoadCategoriesAsync();
    }

    public EditTaskViewModel(ApiService apiService, ILogger<EditTaskViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        UpdateCommand = new Command(async () => await UpdateTaskAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(Title));
        CancelCommand = new Command(async () => await CancelAsync());
        
        InitializePriorityOptions();
        InitializeStatusOptions();
        _ = LoadCategoriesAsync();
    }

    public int TaskId 
    { 
        get => _taskId; 
        set 
        { 
            _taskId = value; 
            OnPropertyChanged(); 
            _ = LoadTaskAsync();
        } 
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)UpdateCommand).ChangeCanExecute();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
            ((Command)UpdateCommand).ChangeCanExecute();
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

    public int SelectedPriorityIndex
    {
        get => _selectedPriorityIndex;
        set
        {
            _selectedPriorityIndex = value;
            OnPropertyChanged();
        }
    }

    public int SelectedStatusIndex
    {
        get => _selectedStatusIndex;
        set
        {
            _selectedStatusIndex = value;
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

    public DateTime MinDate => DateTime.Today;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<string> PriorityOptions { get; } = new();
    public ObservableCollection<string> StatusOptions { get; } = new();

    public ICommand UpdateCommand { get; }
    public ICommand CancelCommand { get; }

    private void InitializePriorityOptions()
    {
        PriorityOptions.Clear();
        foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
        {
            PriorityOptions.Add(priority.ToString());
        }
    }

    private void InitializeStatusOptions()
    {
        StatusOptions.Clear();
        foreach (TaskItemStatus status in Enum.GetValues<TaskItemStatus>())
        {
            StatusOptions.Add(status.ToString());
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
        }
    }

    private async Task LoadTaskAsync()
    {
        if (TaskId <= 0) return;

        IsBusy = true;

        try
        {
            _currentTask = await _apiService.GetTaskByIdAsync(TaskId);
            if (_currentTask != null)
            {
                Title = _currentTask.Title;
                Description = _currentTask.Description ?? string.Empty;
                SelectedPriorityIndex = (int)_currentTask.Priority;
                SelectedStatusIndex = (int)_currentTask.Status;
                
                if (_currentTask.DueDate.HasValue)
                {
                    DueDate = _currentTask.DueDate.Value.Date;
                    DueTime = _currentTask.DueDate.Value.TimeOfDay;
                }
                
                if (_currentTask.EstimatedDuration.HasValue)
                {
                    EstimatedDuration = ((int)_currentTask.EstimatedDuration.Value.TotalMinutes).ToString();
                }

                // Kategoriyi bul ve seç
                await LoadCategoriesAsync();
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == _currentTask.CategoryId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Görev yüklenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Görev yüklenemedi", "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Görev başlığı gereklidir", "Tamam");
            return;
        }

        if (_currentTask == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Güncellenecek görev bulunamadı", "Tamam");
            return;
        }

        IsBusy = true;

        try
        {
            System.Diagnostics.Debug.WriteLine($"EditTaskViewModel: Görev güncelleme başlıyor - ID: {_currentTask.Id}");
            
            var dueDateTime = DueDate.Date.Add(DueTime);
            var estimatedMinutes = int.TryParse(EstimatedDuration, out var minutes) ? minutes : 0;

            _currentTask.Title = Title;
            _currentTask.Description = Description;
            _currentTask.Priority = (TaskPriority)SelectedPriorityIndex;
            _currentTask.Status = (TaskItemStatus)SelectedStatusIndex;
            _currentTask.CategoryId = SelectedCategory?.Id ?? 1;
            _currentTask.DueDate = dueDateTime;
            _currentTask.EstimatedDuration = TimeSpan.FromMinutes(estimatedMinutes);
            _currentTask.UpdatedAt = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"EditTaskViewModel: Güncellenen veriler - Title: {_currentTask.Title}, Status: {_currentTask.Status}, Priority: {_currentTask.Priority}");

            var updatedTask = await _apiService.UpdateTaskAsync(_currentTask);
            
            if (updatedTask != null)
            {
                System.Diagnostics.Debug.WriteLine($"EditTaskViewModel: Görev başarıyla güncellendi");
                
                // Görev başarıyla güncellendi mesajı gönder
                WeakReferenceMessenger.Default.Send(new TaskChangedMessage("TaskUpdated"));
                
                await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Görev başarıyla güncellendi", "Tamam");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"EditTaskViewModel: API'den null response alındı");
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Görev güncellenemedi", "Tamam");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditTaskViewModel: Exception - {ex.Message}");
            _logger?.LogError(ex, "Görev güncellenirken hata oluştu");
            await Application.Current!.MainPage!.DisplayAlert("Hata", $"Görev güncellenemedi: {ex.Message}", "Tamam");
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
