namespace SmartTimeManagement.MAUI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Global exception handler
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException!;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException!;

        MainPage = new AppShell();
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex, "UnhandledException");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "UnobservedTaskException");
        e.SetObserved(); // Prevent app termination
    }

    private void LogException(Exception ex, string type)
    {
        System.Diagnostics.Debug.WriteLine($"[{type}] {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
        }

        // Show user-friendly message
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (MainPage != null)
                {
                    await MainPage.DisplayAlert("Hata", $"Bir hata oluştu: {ex.Message}", "Tamam");
                }
            }
            catch
            {
                // Ignore errors in error handling
            }
        });
    }
}
