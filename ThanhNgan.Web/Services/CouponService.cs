using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Services;

public record CouponResult(Coupon? Coupon, decimal Discount, string? Error);

public interface ICouponService
{
    Task<CouponResult> ApplyAsync(string code, decimal subtotal, IEnumerable<CartItem> items);
}

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _db;
    public CouponService(ApplicationDbContext db) => _db = db;

    public async Task<CouponResult> ApplyAsync(string code, decimal subtotal, IEnumerable<CartItem> items)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
        if (coupon is null) return new CouponResult(null, 0, "Mã giảm giá không tồn tại.");
        if (!coupon.IsValidNow(DateTime.UtcNow)) return new CouponResult(null, 0, "Mã giảm giá đã hết hạn hoặc hết lượt dùng.");
        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
            return new CouponResult(null, 0, $"Đơn hàng cần tối thiểu {coupon.MinOrderAmount:N0}đ để áp dụng mã này.");

        if (coupon.Type == CouponType.ProductSpecific && coupon.AppliedProductId.HasValue)
        {
            var applicable = items.Where(i => i.ProductVariant.ProductId == coupon.AppliedProductId.Value)
                .Sum(i => i.Quantity * i.ProductVariant.EffectivePrice);
            if (applicable <= 0) return new CouponResult(null, 0, "Mã giảm giá không áp dụng cho sản phẩm trong giỏ.");
            var discount = coupon.Value; // số tiền cố định cho sản phẩm chỉ định
            return new CouponResult(coupon, Math.Min(discount, applicable), null);
        }

        var amount = coupon.Type switch
        {
            CouponType.Percentage => subtotal * coupon.Value / 100m,
            CouponType.FixedAmount => coupon.Value,
            _ => 0m
        };
        return new CouponResult(coupon, Math.Min(amount, subtotal), null);
    }
}
