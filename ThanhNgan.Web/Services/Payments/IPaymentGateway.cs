using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Services.Payments;

public record PaymentInitResult(bool Success, string? RedirectUrl, string? Message);

/// <summary>Cổng thanh toán — COD/Chuyển khoản chạy thật ngay; Momo/VnPay/ZaloPay là stub, cắm API key thật qua appsettings:PaymentGateways khi có.</summary>
public interface IPaymentGateway
{
    PaymentMethod Method { get; }
    Task<PaymentInitResult> InitiateAsync(Order order);
}

public class CodPaymentGateway : IPaymentGateway
{
    public PaymentMethod Method => PaymentMethod.Cod;
    public Task<PaymentInitResult> InitiateAsync(Order order) => Task.FromResult(new PaymentInitResult(true, null, "Thanh toán khi nhận hàng (COD)."));
}

public class BankTransferPaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _config;
    public BankTransferPaymentGateway(IConfiguration config) => _config = config;
    public PaymentMethod Method => PaymentMethod.BankTransfer;

    public Task<PaymentInitResult> InitiateAsync(Order order)
    {
        var bank = _config.GetSection("PaymentGateways:BankTransfer");
        var msg = $"Chuyển khoản tới {bank["BankName"]} - STK {bank["AccountNumber"]} - Chủ TK {bank["AccountHolder"]} - Nội dung: {order.Code}";
        return Task.FromResult(new PaymentInitResult(true, null, msg));
    }
}

/// <summary>ponytail: chưa cấu hình API key thật cho Momo/VnPay/ZaloPay — thêm key vào appsettings rồi thay nội dung InitiateAsync bằng gọi API sandbox/production.</summary>
public class StubPaymentGateway : IPaymentGateway
{
    public PaymentMethod Method { get; }
    public StubPaymentGateway(PaymentMethod method) => Method = method;
    public Task<PaymentInitResult> InitiateAsync(Order order) =>
        Task.FromResult(new PaymentInitResult(false, null, $"Cổng thanh toán {Method} chưa được cấu hình. Vui lòng chọn COD hoặc chuyển khoản."));
}

public static class PaymentGatewayFactory
{
    public static IPaymentGateway Resolve(PaymentMethod method, IConfiguration config) => method switch
    {
        PaymentMethod.Cod => new CodPaymentGateway(),
        PaymentMethod.BankTransfer => new BankTransferPaymentGateway(config),
        _ => new StubPaymentGateway(method)
    };
}
