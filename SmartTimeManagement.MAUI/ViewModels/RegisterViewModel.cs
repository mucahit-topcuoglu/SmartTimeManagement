using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartTimeManagement.MAUI.Services;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeManagement.MAUI.ViewModels;

public partial class RegisterViewModel : ObservableValidator
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    [Required(ErrorMessage = "Ad alanı zorunludur")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olmalıdır")]
    private string firstName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Soyad alanı zorunludur")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olmalıdır")]
    private string lastName = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Email alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    private string email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Şifre alanı zorunludur")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    private string password = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    public RegisterViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    // Default constructor for XAML
    public RegisterViewModel() : this(null!)
    {
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsLoading) return;

        if (!ValidateInput()) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTER ATTEMPT STARTED ===");
            System.Diagnostics.Debug.WriteLine($"FirstName: '{FirstName}'");
            System.Diagnostics.Debug.WriteLine($"LastName: '{LastName}'");
            System.Diagnostics.Debug.WriteLine($"Email: '{Email}'");
            System.Diagnostics.Debug.WriteLine($"Password length: {Password?.Length ?? 0}");
            
            var success = await _apiService.RegisterAsync(FirstName, LastName, Email, Password ?? string.Empty);
            
            System.Diagnostics.Debug.WriteLine($"RegisterAsync result: {success}");
            
            if (success)
            {
                SuccessMessage = "Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...";
                await Task.Delay(2000);
                await Shell.Current.GoToAsync("///login");
            }
            else
            {
                ErrorMessage = "Kayıt işlemi başarısız";
            }
        }
        catch (ArgumentException argEx)
        {
            System.Diagnostics.Debug.WriteLine($"=== ARGUMENT EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"Message: {argEx.Message}");
            System.Diagnostics.Debug.WriteLine($"ParamName: {argEx.ParamName}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {argEx.StackTrace}");
            ErrorMessage = $"Geçersiz parametre: {argEx.Message}";
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"=== HTTP EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"Message: {httpEx.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {httpEx.StackTrace}");
            ErrorMessage = $"Bağlantı hatası: {httpEx.Message}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== GENERAL EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"=== INNER EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.InnerException.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.InnerException.StackTrace}");
            }
            
            ErrorMessage = $"Kayıt hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        System.Diagnostics.Debug.WriteLine("=== GO BACK COMMAND CALLED ===");
        try
        {
            await Shell.Current.GoToAsync("//login");
            System.Diagnostics.Debug.WriteLine("Navigation to login successful");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
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

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Şifreler eşleşmiyor";
            return false;
        }

        return true;
    }
}
