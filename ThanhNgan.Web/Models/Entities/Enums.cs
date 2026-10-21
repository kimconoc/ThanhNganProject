namespace ThanhNgan.Web.Models.Entities;

public enum DocumentStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum StockOutType
{
    Sale = 0,
    Damage = 1, // hao hụt: hỏng/hết hạn
    Transfer = 2
}

public enum OrderStatus
{
    PendingConfirmation = 0,
    Processing = 1,
    Shipping = 2,
    Completed = 3,
    Cancelled = 4,
    Returned = 5
}

public enum PaymentMethod
{
    Cod = 0,
    BankTransfer = 1,
    Momo = 2,
    VnPay = 3,
    ZaloPay = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum CustomerGroup
{
    Retail = 0,
    Wholesale = 1,
    Vip = 2
}

public enum CouponType
{
    Percentage = 0,
    FixedAmount = 1,
    ProductSpecific = 2
}

public enum ShippingType
{
    Local = 0,        // giao nội thành
    ColdProvince = 1, // giao tỉnh qua vận chuyển lạnh
    StorePickup = 2
}
