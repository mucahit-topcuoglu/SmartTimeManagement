using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Enums;

namespace SmartTimeManagement.Core.Interfaces;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string email, string password);
    Task<User> RegisterAsync(string firstName, string lastName, string email, string password, UserRole role = UserRole.User);
    Task<User> CreateUserAsync(string firstName, string lastName, string email, string password, UserRole role = UserRole.User);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> UserExistsAsync(string email);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
