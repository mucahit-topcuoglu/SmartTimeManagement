using SmartTimeManagement.Core.Interfaces;
using System.Net;
using System.Net.Mail;

namespace SmartTimeManagement.Data.Services;

public class EmailService : IEmailService
{
    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        string subject = "Şifre Sıfırlama - Smart Time Management";
        string body = $@"
        <h2>Şifre Sıfırlama Talebi</h2>
        <p>Merhaba,</p>
        <p>Şifrenizi sıfırlamak için aşağıdaki kodu kullanın:</p>
        <h3 style='color: #007ACC; background: #f0f0f0; padding: 10px; text-align: center;'>{resetToken}</h3>
        <p>Bu kod 30 dakika boyunca geçerlidir.</p>
        <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
        <p>İyi günler dileriz,<br/>Smart Time Management Ekibi</p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Demo amaçlı basit implementation
        // Gerçek uygulamada SMTP ayarları yapılmalı
        try
        {
            // Burada gerçek email gönderme işlemi yapılır
            // Şimdilik konsola yazdırıyoruz
            Console.WriteLine($"Email gönderiliyor: {to}");
            Console.WriteLine($"Konu: {subject}");
            Console.WriteLine($"İçerik: {body}");
            
            // Simulated email sending delay
            await Task.Delay(1000);
            
            Console.WriteLine("Email başarıyla gönderildi (simulated)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email gönderme hatası: {ex.Message}");
            throw;
        }
    }
}
