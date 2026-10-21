using Microsoft.AspNetCore.Identity.UI.Services;

namespace ThanhNgan.Web.Services;

/// <summary>ponytail: chưa nối SMTP thật — Identity (quên mật khẩu, xác nhận email) chỉ log lại link thay vì gửi mail. Cắm SMTP/SendGrid khi có.</summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("[Email giả lập] Tới: {Email} - Tiêu đề: {Subject}\n{Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
