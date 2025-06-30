using SmartTimeManagement.MAUI.ViewModels;

namespace SmartTimeManagement.MAUI.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
