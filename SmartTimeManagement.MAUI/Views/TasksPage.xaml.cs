using SmartTimeManagement.MAUI.ViewModels;

namespace SmartTimeManagement.MAUI.Views;

public partial class TasksPage : ContentPage
{
    public TasksPage()
    {
        InitializeComponent();
    }

    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
