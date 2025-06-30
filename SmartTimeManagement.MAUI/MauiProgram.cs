using Microsoft.Extensions.Logging;
using SmartTimeManagement.MAUI.Services;
using SmartTimeManagement.MAUI.ViewModels;
using SmartTimeManagement.MAUI.Views;

namespace SmartTimeManagement.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register Services
		builder.Services.AddSingleton<ApiService>();
		builder.Services.AddSingleton<UserSessionService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<TasksViewModel>();
		builder.Services.AddTransient<AddTaskViewModel>();
		builder.Services.AddTransient<EditTaskViewModel>();
		builder.Services.AddTransient<RemindersViewModel>();
		builder.Services.AddTransient<AddReminderViewModel>();
		builder.Services.AddTransient<EditReminderViewModel>();
		builder.Services.AddTransient<ReportsViewModel>();
		builder.Services.AddTransient<ChangePasswordViewModel>();

		// Register Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<TasksPage>();
		builder.Services.AddTransient<AddTaskPage>();
		builder.Services.AddTransient<EditTaskPage>();
		builder.Services.AddTransient<RemindersPage>();
		builder.Services.AddTransient<AddReminderPage>();
		builder.Services.AddTransient<EditReminderPage>();
		builder.Services.AddTransient<ReportsPage>();
		builder.Services.AddTransient<ChangePasswordPage>();

#if DEBUG
		builder.Logging.AddDebug();
		
		// Add debug logging to track exceptions
		builder.Services.AddLogging(logging =>
		{
			logging.AddDebug();
			logging.SetMinimumLevel(LogLevel.Debug);
		});
#endif

		return builder.Build();
	}
}
