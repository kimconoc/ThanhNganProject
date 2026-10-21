using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Services;

public record StockLine(int ProductVariantId, int Quantity, decimal? ImportPrice = null, DateTime? ExpiryDate = null, string? BatchCode = null);

/// <summary>
/// Nghiệp vụ kho: nhập/xuất/kiểm kê, tính giá vốn FIFO (lô nhập trước xuất trước — đúng đặc thù hải sản).
/// Phiếu giá trị &gt;= ApprovalThreshold phải chờ Quản lý duyệt mới có hiệu lực lên tồn kho (xem CreateStockInAsync/CreateStockOutAsync).
/// </summary>
public class InventoryService
{
    public const decimal ApprovalThreshold = 5_000_000m; // ponytail: ngưỡng "giá trị lớn" tạm cố định, có thể chuyển sang SystemSetting khi cần cấu hình qua UI.

    private readonly ApplicationDbContext _db;
    public InventoryService(ApplicationDbContext db) => _db = db;

    private static string NewCode(string prefix) => $"{prefix}{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}";

    public async Task<StockInReceipt> CreateStockInAsync(int supplierId, string userId, List<StockLine> lines, string? note)
    {
        var totalValue = lines.Sum(l => l.Quantity * (l.ImportPrice ?? 0));
        var receipt = new StockInReceipt
        {
            Code = NewCode("PN"),
            SupplierId = supplierId,
            CreatedByUserId = userId,
            Note = note,
            Status = totalValue >= ApprovalThreshold ? DocumentStatus.PendingApproval : DocumentStatus.Approved
        };
        foreach (var line in lines)
            receipt.Details.Add(new StockInReceiptDetail
            {
                ProductVariantId = line.ProductVariantId,
                Quantity = line.Quantity,
                ImportPrice = line.ImportPrice ?? 0,
                ExpiryDate = line.ExpiryDate,
                BatchCode = line.BatchCode ?? NewCode("LO")
            });

        _db.StockInReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        if (receipt.Status == DocumentStatus.Approved)
            await ApplyStockInEffectAsync(receipt);

        return receipt;
    }

    public async Task ApproveStockInAsync(int receiptId, string approverUserId)
    {
        var receipt = await _db.StockInReceipts.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == receiptId)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập.");
        if (receipt.Status != DocumentStatus.PendingApproval) throw new InvalidOperationException("Phiếu không ở trạng thái chờ duyệt.");

        receipt.Status = DocumentStatus.Approved;
        receipt.ApprovedByUserId = approverUserId;
        receipt.ApprovedAt = DateTime.UtcNow;
        await ApplyStockInEffectAsync(receipt);
        await _db.SaveChangesAsync();
    }

    private async Task ApplyStockInEffectAsync(StockInReceipt receipt)
    {
        foreach (var detail in receipt.Details)
        {
            _db.InventoryBatches.Add(new InventoryBatch
            {
                ProductVariantId = detail.ProductVariantId,
                SupplierId = receipt.SupplierId,
                StockInReceiptDetailId = detail.Id,
                BatchCode = detail.BatchCode,
                ImportDate = receipt.CreatedAt,
                ExpiryDate = detail.ExpiryDate,
                ImportPrice = detail.ImportPrice,
                QuantityImported = detail.Quantity,
                QuantityRemaining = detail.Quantity
            });

            var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
            if (variant is not null) variant.StockQuantity += detail.Quantity;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<StockOutReceipt> CreateStockOutAsync(StockOutType type, string userId, List<StockLine> lines,
        int? orderId = null, string? note = null, string? targetWarehouse = null)
    {
        // Xuất bán tự động khi đơn hàng được xác nhận -> luôn có hiệu lực ngay (hệ thống sinh, không phải nhập tay).
        // Xuất hủy/hao hụt hoặc chuyển kho do nhân viên nhập tay -> giá trị lớn phải chờ duyệt.
        decimal totalValue = 0;
        if (type != StockOutType.Sale)
            foreach (var l in lines)
                totalValue += l.Quantity * await EstimateUnitCostAsync(l.ProductVariantId);

        var receipt = new StockOutReceipt
        {
            Code = NewCode("PX"),
            Type = type,
            OrderId = orderId,
            CreatedByUserId = userId,
            Note = note,
            TargetWarehouse = targetWarehouse,
            Status = (type != StockOutType.Sale && totalValue >= ApprovalThreshold) ? DocumentStatus.PendingApproval : DocumentStatus.Approved
        };
        foreach (var line in lines)
            receipt.Details.Add(new StockOutReceiptDetail { ProductVariantId = line.ProductVariantId, Quantity = line.Quantity });

        _db.StockOutReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        if (receipt.Status == DocumentStatus.Approved)
            await ApplyStockOutEffectAsync(receipt);

        return receipt;
    }

    public async Task ApproveStockOutAsync(int receiptId, string approverUserId)
    {
        var receipt = await _db.StockOutReceipts.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == receiptId)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu xuất.");
        if (receipt.Status != DocumentStatus.PendingApproval) throw new InvalidOperationException("Phiếu không ở trạng thái chờ duyệt.");

        receipt.Status = DocumentStatus.Approved;
        receipt.ApprovedByUserId = approverUserId;
        receipt.ApprovedAt = DateTime.UtcNow;
        await ApplyStockOutEffectAsync(receipt);
        await _db.SaveChangesAsync();
    }

    private async Task ApplyStockOutEffectAsync(StockOutReceipt receipt)
    {
        foreach (var detail in receipt.Details)
        {
            var allocations = await AllocateFifoAsync(detail.ProductVariantId, detail.Quantity);
            decimal totalCost = 0;
            foreach (var (batch, qty) in allocations)
            {
                _db.StockOutAllocations.Add(new StockOutAllocation
                {
                    StockOutReceiptDetailId = detail.Id,
                    InventoryBatchId = batch.Id,
                    Quantity = qty,
                    UnitCost = batch.ImportPrice
                });
                batch.QuantityRemaining -= qty;
                totalCost += qty * batch.ImportPrice;
            }
            detail.UnitCost = detail.Quantity > 0 ? totalCost / detail.Quantity : 0;

            var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
            if (variant is not null) variant.StockQuantity -= detail.Quantity;
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>Phân bổ số lượng cần xuất vào các lô còn tồn theo FIFO (lô nhập sớm nhất trước). Ném lỗi nếu không đủ tồn kho.</summary>
    private async Task<List<(InventoryBatch batch, int qty)>> AllocateFifoAsync(int productVariantId, int quantity)
    {
        var batches = await _db.InventoryBatches
            .Where(b => b.ProductVariantId == productVariantId && b.QuantityRemaining > 0)
            .OrderBy(b => b.ImportDate).ThenBy(b => b.Id)
            .ToListAsync();

        var result = new List<(InventoryBatch, int)>();
        var remaining = quantity;
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, batch.QuantityRemaining);
            result.Add((batch, take));
            remaining -= take;
        }

        if (remaining > 0)
            throw new InvalidOperationException($"Không đủ tồn kho theo lô FIFO cho biến thể #{productVariantId} (thiếu {remaining}).");

        return result;
    }

    private async Task<decimal> EstimateUnitCostAsync(int productVariantId)
    {
        var batch = await _db.InventoryBatches
            .Where(b => b.ProductVariantId == productVariantId && b.QuantityRemaining > 0)
            .OrderBy(b => b.ImportDate).FirstOrDefaultAsync();
        return batch?.ImportPrice ?? 0;
    }

    /// <summary>Kiểm kê: lệch dương coi như phát hiện thêm hàng (tạo lô mới giá = giá vốn FIFO hiện tại), lệch âm trừ tồn theo FIFO (hao hụt).</summary>
    public async Task ApproveAdjustmentAsync(int adjustmentId, string approverUserId)
    {
        var adjustment = await _db.StockAdjustments.Include(a => a.Details).FirstOrDefaultAsync(a => a.Id == adjustmentId)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu kiểm kê.");
        if (adjustment.Status != DocumentStatus.PendingApproval) throw new InvalidOperationException("Phiếu không ở trạng thái chờ duyệt.");

        foreach (var detail in adjustment.Details)
        {
            if (detail.Difference == 0) continue;
            var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
            if (detail.Difference > 0)
            {
                var cost = await EstimateUnitCostAsync(detail.ProductVariantId);
                _db.InventoryBatches.Add(new InventoryBatch
                {
                    ProductVariantId = detail.ProductVariantId,
                    BatchCode = NewCode("KK"),
                    ImportDate = DateTime.UtcNow,
                    ImportPrice = cost,
                    QuantityImported = detail.Difference,
                    QuantityRemaining = detail.Difference
                });
                if (variant is not null) variant.StockQuantity += detail.Difference;
            }
            else
            {
                var shortQty = -detail.Difference;
                var allocations = await AllocateFifoAsync(detail.ProductVariantId, shortQty);
                foreach (var (batch, qty) in allocations) batch.QuantityRemaining -= qty;
                if (variant is not null) variant.StockQuantity -= shortQty;
            }
        }

        adjustment.Status = DocumentStatus.Approved;
        adjustment.ApprovedByUserId = approverUserId;
        adjustment.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
