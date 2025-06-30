using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;

namespace SmartTimeManagement.MAUI.Services;

public class UserSessionService
{
    private static UserSessionService? _instance;
    private static readonly object _lock = new object();

    private User? _currentUser;
    private bool _isLoggedIn = false;

    // Singleton Instance
    public static UserSessionService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new UserSessionService();
                }
            }
            return _instance;
        }
    }

    // Private constructor to prevent external instantiation
    private UserSessionService()
    {
    }

    public User? CurrentUser 
    { 
        get => _currentUser; 
        private set 
        { 
            _currentUser = value;
            _isLoggedIn = value != null;
        } 
    }

    public bool IsLoggedIn => _isLoggedIn;

    public event EventHandler<User?>? UserChanged;

    public void SetUser(User user)
    {
        System.Diagnostics.Debug.WriteLine($"UserSessionService: Setting user {user.Id} ({user.Email})");
        CurrentUser = user;
        System.Diagnostics.Debug.WriteLine("UserSessionService: User session set");
        UserChanged?.Invoke(this, user);
        System.Diagnostics.Debug.WriteLine("UserSessionService: UserChanged event fired");
    }

    public void Logout()
    {
        System.Diagnostics.Debug.WriteLine($"UserSessionService: Logging out user {CurrentUser?.Id} ({CurrentUser?.Email})");
        CurrentUser = null;
        System.Diagnostics.Debug.WriteLine("UserSessionService: User session cleared");
        UserChanged?.Invoke(this, null);
        System.Diagnostics.Debug.WriteLine("UserSessionService: UserChanged event fired");
    }

    public bool IsAdmin()
    {
        return CurrentUser?.Role == UserRole.Admin;
    }
}
