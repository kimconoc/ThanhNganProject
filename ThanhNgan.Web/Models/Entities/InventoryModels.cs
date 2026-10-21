namespace ThanhNgan.Web.Models.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // ngư dân, vựa hải sản, cơ sở làm mắm...
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Một lô hàng nhập kho còn tồn — đơn vị tính giá vốn FIFO.</summary>
public class InventoryBatch
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int StockInReceiptDetailId { get; set; }
    public StockInReceiptDetail StockInReceiptDetail { get; set; } = null!;
    public string BatchCode { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal ImportPrice { get; set; } // giá nhập lô này (biến động theo ngày/mùa vụ)
    public int QuantityImported { get; set; }
    public int QuantityRemaining { get; set; } // giảm dần khi xuất theo FIFO
}

public class StockInReceipt
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.Approved; // nhập kho có hiệu lực ngay trừ khi giá trị lớn (đánh dấu PendingApproval thủ công)
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<StockInReceiptDetail> Details { get; set; } = new List<StockInReceiptDetail>();
}

public class StockInReceiptDetail
{
    public int Id { get; set; }
    public int StockInReceiptId { get; set; }
    public StockInReceipt StockInReceipt { get; set; } = null!;
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string BatchCode { get; set; } = string.Empty;
}

public class StockOutReceipt
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public StockOutType Type { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.Approved;
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Note { get; set; }
    public string? TargetWarehouse { get; set; } // dùng cho xuất chuyển kho

    public ICollection<StockOutReceiptDetail> Details { get; set; } = new List<StockOutReceiptDetail>();
}

public class StockOutReceiptDetail
{
    public int Id { get; set; }
    public int StockOutReceiptId { get; set; }
    public StockOutReceipt StockOutReceipt { get; set; } = null!;
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }

    /// <summary>Giá vốn bình quân theo các lô FIFO đã phân bổ cho dòng này.</summary>
    public decimal UnitCost { get; set; }

    public ICollection<StockOutAllocation> Allocations { get; set; } = new List<StockOutAllocation>();
}

/// <summary>Phân bổ 1 dòng xuất kho vào (các) lô nhập cụ thể theo FIFO — nguồn cho báo cáo giá vốn/lời-lỗ.</summary>
public class StockOutAllocation
{
    public int Id { get; set; }
    public int StockOutReceiptDetailId { get; set; }
    public StockOutReceiptDetail StockOutReceiptDetail { get; set; } = null!;
    public int InventoryBatchId { get; set; }
    public InventoryBatch InventoryBatch { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; } // = InventoryBatch.ImportPrice tại thời điểm xuất
}

public class StockAdjustment
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DocumentStatus Status { get; set; } = DocumentStatus.PendingApproval; // kiểm kê luôn cần Quản lý duyệt
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<StockAdjustmentDetail> Details { get; set; } = new List<StockAdjustmentDetail>();
}

public class StockAdjustmentDetail
{
    public int Id { get; set; }
    public int StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; } = null!;
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int SystemQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public int Difference => ActualQuantity - SystemQuantity;
}
