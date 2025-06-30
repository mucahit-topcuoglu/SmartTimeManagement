using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartTimeManagement.MAUI.Services;
using SmartTimeManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.MAUI.ViewModels;

public partial class LoginViewModel : ObservableValidator
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    [Required(ErrorMessage = "Email alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    private string email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Şifre alanı zorunludur")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public LoginViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    // Default constructor for XAML
    public LoginViewModel() : this(new ApiService())
    {
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        if (!ValidateInput()) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var user = await _apiService.LoginAsync(Email, Password);
            if (user != null)
            {
                UserSessionService.Instance.SetUser(user);
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                ErrorMessage = "Geçersiz email veya şifre";
            }
        }
        catch (HttpRequestException httpEx)
        {
            ErrorMessage = $"Bağlantı hatası: API sunucusuna erişilemiyor. {httpEx.Message}";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Giriş hatası: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Login error details: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("///register");
    }



    private bool ValidateInput()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(this, context, results, true))
        {
            ErrorMessage = results.First().ErrorMessage ?? "Geçersiz giriş";
            return false;
        }

        return true;
    }
}
