using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SmartTimeManagement.MAUI.Services;

namespace SmartTimeManagement.MAUI.ViewModels;

public class ChangePasswordViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly ILogger<ChangePasswordViewModel> _logger;
    private bool _isBusy;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    public ChangePasswordViewModel()
    {
        _apiService = new ApiService();
        _logger = null!; // DI ile çözülecek
        
        SaveCommand = new Command(async () => await ChangePasswordAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
    }

    public ChangePasswordViewModel(ApiService apiService, ILogger<ChangePasswordViewModel> logger)
    {
        _apiService = apiService;
        _logger = logger;
        
        SaveCommand = new Command(async () => await ChangePasswordAsync(), CanSave);
        CancelCommand = new Command(async () => await CancelAsync());
    }

    private bool CanSave()
    {
        return !IsBusy && 
               !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword) &&
               NewPassword == ConfirmPassword &&
               NewPassword.Length >= 6;
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

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            _currentPassword = value;
            OnPropertyChanged();
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            _newPassword = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPasswordValid));
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            _confirmPassword = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPasswordValid));
            OnPropertyChanged(nameof(PasswordsMatch));
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    public bool IsPasswordValid => NewPassword.Length >= 6;
    public bool PasswordsMatch => NewPassword == ConfirmPassword;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Mevcut şifre gereklidir", "Tamam");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Yeni şifre gereklidir", "Tamam");
            return;
        }

        if (NewPassword.Length < 6)
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Yeni şifre en az 6 karakter olmalıdır", "Tamam");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Yeni şifreler eşleşmiyor", "Tamam");
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
            var result = await _apiService.ChangePasswordAsync(userSession.CurrentUser.Id, CurrentPassword, NewPassword);
            
            if (result)
            {
                await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Şifre başarıyla değiştirildi", "Tamam");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Şifre değiştirilemedi. Mevcut şifrenizi kontrol edin.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Şifre değiştirme hatası");
            await Application.Current!.MainPage!.DisplayAlert("Hata", "Şifre değiştirilemedi: " + ex.Message, "Tamam");
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
