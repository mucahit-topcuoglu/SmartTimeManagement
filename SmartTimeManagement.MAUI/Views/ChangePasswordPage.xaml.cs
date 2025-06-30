using SmartTimeManagement.MAUI.ViewModels;

namespace SmartTimeManagement.MAUI.Views;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage()
    {
        InitializeComponent();
        BindingContext = new ChangePasswordViewModel();
    }

    public ChangePasswordPage(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
