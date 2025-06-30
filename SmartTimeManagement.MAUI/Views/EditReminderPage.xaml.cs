using SmartTimeManagement.MAUI.ViewModels;

namespace SmartTimeManagement.MAUI.Views;

public partial class EditReminderPage : ContentPage
{
    public EditReminderPage()
    {
        InitializeComponent();
        BindingContext = new EditReminderViewModel();
    }

    public EditReminderPage(EditReminderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
