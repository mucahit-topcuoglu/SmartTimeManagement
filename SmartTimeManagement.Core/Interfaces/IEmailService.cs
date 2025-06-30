namespace SmartTimeManagement.Core.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken);
    Task SendEmailAsync(string to, string subject, string body);
}
