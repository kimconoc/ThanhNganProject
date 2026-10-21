namespace ThanhNgan.Web.Models.Entities;

/// <summary>Ghi log mọi thao tác tạo/sửa/xóa trên các bảng nghiệp vụ quan trọng — không cho sửa/xóa âm thầm.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create / Update / Delete
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Changes { get; set; } // JSON: { field: { old, new } }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>Cấu hình hệ thống dạng key-value: bật/tắt cổng thanh toán, phí ship mặc định...</summary>
public class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
