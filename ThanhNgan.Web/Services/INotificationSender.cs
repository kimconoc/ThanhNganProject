using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Services;

/// <summary>ponytail: chưa nối SMTP/SMS thật — hiện chỉ log lại. Cắm thật qua MailKit/eSMS/Twilio khi có thông tin dịch vụ.</summary>
public interface INotificationSender
{
    Task SendOrderConfirmationAsync(Order order);
}

public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;
    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) => _logger = logger;

    public Task SendOrderConfirmationAsync(Order order)
    {
        _logger.LogInformation("[Email/SMS xác nhận đơn hàng] Đơn {Code} - KH {Name} ({Phone}) - Tổng tiền {Total:N0}đ",
            order.Code, order.RecipientName, order.RecipientPhone, order.TotalAmount);
        return Task.CompletedTask;
    }
}
