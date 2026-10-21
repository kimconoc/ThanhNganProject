namespace ThanhNgan.Web.Data;

public static class RoleNames
{
    public const string Customer = "Customer";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string SalesStaff = "SalesStaff";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly string[] All = { Customer, WarehouseStaff, SalesStaff, Manager, Admin };

    /// <summary>Các role được vào khu vực Admin (nhân viên trở lên).</summary>
    public const string StaffPolicy = $"{WarehouseStaff},{SalesStaff},{Manager},{Admin}";

    /// <summary>Role được duyệt phiếu / sửa-xóa phiếu đã lưu / xem báo cáo lời-lỗ / quản lý sản phẩm-giá.</summary>
    public const string ManagerPolicy = $"{Manager},{Admin}";

    /// <summary>Role được tạo phiếu nhập/xuất kho.</summary>
    public const string WarehousePolicy = $"{WarehouseStaff},{Manager},{Admin}";

    /// <summary>Role được xem tồn kho (mọi nhân viên).</summary>
    public const string ViewStockPolicy = StaffPolicy;
}
