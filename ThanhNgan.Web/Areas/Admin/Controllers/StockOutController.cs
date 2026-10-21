using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Areas.Admin.Models;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;
using ThanhNgan.Web.Services;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.WarehousePolicy)]
public class StockOutController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryService _inventoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StockOutController(ApplicationDbContext db, InventoryService inventoryService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _inventoryService = inventoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var receipts = await _db.StockOutReceipts.Include(r => r.Details)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(receipts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var receipt = await _db.StockOutReceipts
            .Include(r => r.Details).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (receipt is null) return NotFound();
        return View(receipt);
    }

    // Chỉ cho tạo tay loại Hủy/hao hụt hoặc Chuyển kho — xuất bán do hệ thống tự sinh khi xác nhận đơn (xem OrdersController).
    public async Task<IActionResult> Create()
    {
        ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
        return View(new StockOutForm { Type = StockOutType.Damage, Lines = { new StockLineForm() } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockOutForm form)
    {
        if (form.Type == StockOutType.Sale) ModelState.AddModelError("", "Xuất bán do hệ thống tự sinh khi xác nhận đơn hàng, không tạo tay ở đây.");
        form.Lines = form.Lines?.Where(l => l.ProductVariantId > 0 && l.Quantity > 0).ToList() ?? new();
        if (form.Lines.Count == 0) ModelState.AddModelError("", "Vui lòng nhập ít nhất một dòng sản phẩm.");

        if (!ModelState.IsValid)
        {
            ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
            return View(form);
        }

        var userId = _userManager.GetUserId(User)!;
        var lines = form.Lines.Select(l => new StockLine(l.ProductVariantId, l.Quantity)).ToList();
        try
        {
            var receipt = await _inventoryService.CreateStockOutAsync(form.Type, userId, lines, note: form.Note, targetWarehouse: form.TargetWarehouse);
            TempData["AdminMessage"] = receipt.Status == DocumentStatus.PendingApproval
                ? $"Đã tạo phiếu xuất {receipt.Code} — giá trị lớn, cần Quản lý duyệt trước khi trừ tồn kho."
                : $"Đã tạo phiếu xuất {receipt.Code} và trừ tồn kho.";
            return RedirectToAction(nameof(Details), new { id = receipt.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
            return View(form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerPolicy)]
    public async Task<IActionResult> Approve(int id)
    {
        await _inventoryService.ApproveStockOutAsync(id, _userManager.GetUserId(User)!);
        TempData["AdminMessage"] = "Đã duyệt phiếu xuất, tồn kho đã được cập nhật.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var receipt = await _db.StockOutReceipts.FindAsync(id);
        if (receipt is null) return NotFound();
        if (receipt.Status != DocumentStatus.PendingApproval)
        {
            TempData["AdminMessage"] = "Chỉ hủy được phiếu đang chờ duyệt.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.StockOutReceipts.Remove(receipt); // chưa duyệt nên chưa trừ tồn kho, xóa an toàn
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã hủy phiếu xuất.";
        return RedirectToAction(nameof(Index));
    }
}
