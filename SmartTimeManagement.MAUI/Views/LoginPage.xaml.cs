using SmartTimeManagement.MAUI.ViewModels;

namespace SmartTimeManagement.MAUI.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
