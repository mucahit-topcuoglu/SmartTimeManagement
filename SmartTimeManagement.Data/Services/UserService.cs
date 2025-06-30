using Microsoft.EntityFrameworkCore;
using SmartTimeManagement.Core.Entities;
using SmartTimeManagement.Core.Interfaces;
using SmartTimeManagement.Core.Enums;
using SmartTimeManagement.Data.Context;

namespace SmartTimeManagement.Data.Services;

public class UserService : IUserService
{
    private readonly SmartTimeManagementDbContext _context;

    public UserService(SmartTimeManagementDbContext context)
    {
        _context = context;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        
        if (user != null && VerifyPassword(password, user.PasswordHash))
        {
            return user;
        }
        
        return null;
    }

    public async Task<User> RegisterAsync(string firstName, string lastName, string email, string password, UserRole role = UserRole.User)
    {
        if (await UserExistsAsync(email))
        {
            throw new InvalidOperationException("Bu email adresi zaten kullanılıyor.");
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = email
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User> CreateUserAsync(string firstName, string lastName, string email, string password, UserRole role = UserRole.User)
    {
        if (await UserExistsAsync(email))
        {
            throw new InvalidOperationException("Bu email adresi zaten kullanılıyor.");
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = email
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.Where(u => u.IsActive).ToListAsync();
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null && VerifyPassword(currentPassword, user.PasswordHash))
        {
            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
