using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Models.ViewModels;

/// <summary>Tên hiển thị tiếng Việt cho các enum nghiệp vụ — dùng ở mọi view thay vì in trực tiếp tên enum (tiếng Anh).</summary>
public static class EnumDisplay
{
    public static string ToDisplay(this OrderStatus status) => status switch
    {
        OrderStatus.PendingConfirmation => "Chờ xác nhận",
        OrderStatus.Processing => "Đang xử lý",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Returned => "Đã hoàn trả",
        _ => status.ToString()
    };

    public static string ToDisplay(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cod => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        PaymentMethod.Momo => "Ví Momo",
        PaymentMethod.VnPay => "VNPay",
        PaymentMethod.ZaloPay => "ZaloPay",
        _ => method.ToString()
    };

    public static string ToDisplay(this PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Failed => "Thanh toán thất bại",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => status.ToString()
    };

    public static string ToDisplay(this CustomerGroup group) => group switch
    {
        CustomerGroup.Retail => "Khách lẻ",
        CustomerGroup.Wholesale => "Khách sỉ",
        CustomerGroup.Vip => "Khách VIP",
        _ => group.ToString()
    };

    public static string ToDisplay(this DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => "Nháp",
        DocumentStatus.PendingApproval => "Chờ duyệt",
        DocumentStatus.Approved => "Đã duyệt",
        DocumentStatus.Rejected => "Từ chối",
        _ => status.ToString()
    };

    public static string ToDisplay(this StockOutType type) => type switch
    {
        StockOutType.Sale => "Xuất bán",
        StockOutType.Damage => "Hủy / hao hụt",
        StockOutType.Transfer => "Chuyển kho",
        _ => type.ToString()
    };

    public static string ToDisplay(this ShippingType type) => type switch
    {
        ShippingType.Local => "Giao nội thành",
        ShippingType.ColdProvince => "Giao tỉnh (xe lạnh)",
        ShippingType.StorePickup => "Nhận tại cửa hàng",
        _ => type.ToString()
    };

    public static string ToDisplay(this CouponType type) => type switch
    {
        CouponType.Percentage => "Giảm theo %",
        CouponType.FixedAmount => "Giảm số tiền cố định",
        CouponType.ProductSpecific => "Giảm theo sản phẩm",
        _ => type.ToString()
    };

    /// <summary>Tên vai trò (role code nội bộ -> tiếng Việt hiển thị).</summary>
    public static string RoleToDisplay(string roleCode) => roleCode switch
    {
        "Customer" => "Khách hàng",
        "WarehouseStaff" => "Nhân viên kho",
        "SalesStaff" => "Nhân viên bán hàng",
        "Manager" => "Quản lý",
        "Admin" => "Admin",
        _ => roleCode
    };
}
