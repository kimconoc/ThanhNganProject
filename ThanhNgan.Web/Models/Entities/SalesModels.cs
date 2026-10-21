namespace ThanhNgan.Web.Models.Entities;

public class Cart
{
    public int Id { get; set; }
    public string? UserId { get; set; } // null nếu khách vãng lai
    public string? SessionId { get; set; } // dùng khi chưa đăng nhập
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
}

public class ShippingMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShippingType Type { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; } // % hoặc số tiền
    public decimal? MinOrderAmount { get; set; }
    public int? AppliedProductId { get; set; } // dùng khi Type = ProductSpecific
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsValidNow(DateTime now) =>
        IsActive && now >= StartDate && now <= EndDate && (!UsageLimit.HasValue || UsedCount < UsageLimit.Value);
}

public class Order
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? UserId { get; set; } // null nếu Guest Checkout

    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;

    public int ShippingMethodId { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = null!;
    public decimal ShippingFee { get; set; }

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    // Snapshot tại thời điểm đặt hàng — không phụ thuộc Product bị sửa sau này
    public string ProductName { get; set; } = string.Empty;
    public string PackagingSpec { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }
}
