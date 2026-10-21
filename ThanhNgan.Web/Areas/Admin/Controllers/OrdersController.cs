using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;
using ThanhNgan.Web.Services;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

public class OrdersController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryService _inventoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(ApplicationDbContext db, InventoryService inventoryService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _inventoryService = inventoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(OrderStatus? status)
    {
        var query = _db.Orders.Include(o => o.Details).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status);
        ViewBag.StatusFilter = status;
        return View(await query.OrderByDescending(o => o.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.Include(o => o.Details).ThenInclude(d => d.ProductVariant)
            .Include(o => o.ShippingMethod).Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var order = await _db.Orders.Include(o => o.Details).ThenInclude(d => d.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.PendingConfirmation)
        {
            TempData["AdminMessage"] = "Đơn hàng không ở trạng thái chờ xác nhận.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = _userManager.GetUserId(User)!;
        var lines = order.Details.Select(d => new StockLine(d.ProductVariantId, d.Quantity)).ToList();
        try
        {
            await _inventoryService.CreateStockOutAsync(StockOutType.Sale, userId, lines, orderId: order.Id,
                note: $"Xuất bán tự động cho đơn {order.Code}");
            order.Status = OrderStatus.Processing;
            await _db.SaveChangesAsync();
            TempData["AdminMessage"] = "Đã xác nhận đơn hàng và xuất kho tự động.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["AdminMessage"] = $"Không thể xác nhận: {ex.Message}";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ponytail: "Hoàn trả" chỉ đổi trạng thái đơn, chưa tự tạo phiếu nhập trả hàng vào kho —
    // nhân viên kho tạo phiếu nhập riêng (StockIn) cho hàng hoàn trả còn bán được, khi cần bổ sung tự động hoá sau.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        order.Status = status;
        if (status == OrderStatus.Completed) order.PaymentStatus = order.PaymentStatus == PaymentStatus.Pending && order.PaymentMethod == PaymentMethod.Cod
            ? PaymentStatus.Paid : order.PaymentStatus;
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã cập nhật trạng thái đơn hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.PendingConfirmation)
        {
            TempData["AdminMessage"] = "Chỉ có thể hủy đơn khi đang chờ xác nhận (đơn đã xuất kho cần xử lý hoàn trả).";
            return RedirectToAction(nameof(Details), new { id });
        }
        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã hủy đơn hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var order = await _db.Orders.Include(o => o.Details).Include(o => o.ShippingMethod).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(20);
                page.Header().Text($"HÓA ĐƠN BÁN HÀNG - {order.Code}").FontSize(16).Bold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"Ngày: {order.CreatedAt:dd/MM/yyyy HH:mm}");
                    col.Item().Text($"Khách hàng: {order.RecipientName} - {order.RecipientPhone}");
                    col.Item().Text($"Địa chỉ: {order.ShippingAddress}");
                    col.Item().PaddingTop(10).LineHorizontal(1);
                    foreach (var d in order.Details)
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{d.ProductName} ({d.PackagingSpec}) x{d.Quantity}");
                            row.ConstantItem(100).AlignRight().Text($"{d.LineTotal:N0}đ");
                        });
                    col.Item().PaddingTop(10).LineHorizontal(1);
                    col.Item().AlignRight().Text($"Tạm tính: {order.SubTotal:N0}đ");
                    col.Item().AlignRight().Text($"Giảm giá: -{order.DiscountAmount:N0}đ");
                    col.Item().AlignRight().Text($"Phí vận chuyển: {order.ShippingFee:N0}đ");
                    col.Item().AlignRight().Text($"TỔNG CỘNG: {order.TotalAmount:N0}đ").Bold().FontSize(13);
                });
                page.Footer().AlignCenter().Text("Cảm ơn quý khách đã mua hàng tại Thanh Ngân Seafood!");
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"HoaDon-{order.Code}.pdf");
    }
}
