using SmartTimeManagement.MAUI.Views;

namespace SmartTimeManagement.MAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();
	}

	private void RegisterRoutes()
	{
		// Task related routes
		Routing.RegisterRoute("tasks/add", typeof(AddTaskPage));
		Routing.RegisterRoute("tasks/edit", typeof(EditTaskPage));
		Routing.RegisterRoute("//tasks/add", typeof(AddTaskPage));
		Routing.RegisterRoute("//tasks/edit", typeof(EditTaskPage));
		
		// Reminder related routes
		Routing.RegisterRoute("reminders/add", typeof(AddReminderPage));
		Routing.RegisterRoute("reminders/edit", typeof(EditReminderPage));
		Routing.RegisterRoute("//reminders/add", typeof(AddReminderPage));
		Routing.RegisterRoute("//reminders/edit", typeof(EditReminderPage));
		
		// User related routes
		Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));
		Routing.RegisterRoute("//change-password", typeof(ChangePasswordPage));
		
		// Main navigation routes - these should match the Shell structure
		Routing.RegisterRoute("main/tasks", typeof(TasksPage));
		Routing.RegisterRoute("main/reminders", typeof(RemindersPage));
		Routing.RegisterRoute("main/reports", typeof(ReportsPage));
	}
}
