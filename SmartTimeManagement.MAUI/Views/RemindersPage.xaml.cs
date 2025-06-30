using SmartTimeManagement.MAUI.ViewModels;

namespace SmartTimeManagement.MAUI.Views;

public partial class RemindersPage : ContentPage
{
    private RemindersViewModel _viewModel;

    public RemindersPage()
    {
        InitializeComponent();
        _viewModel = new RemindersViewModel();
        BindingContext = _viewModel;
    }

    public RemindersPage(RemindersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
