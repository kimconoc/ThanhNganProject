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
public class StockInController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryService _inventoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StockInController(ApplicationDbContext db, InventoryService inventoryService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _inventoryService = inventoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var receipts = await _db.StockInReceipts.Include(r => r.Supplier).Include(r => r.Details)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(receipts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var receipt = await _db.StockInReceipts.Include(r => r.Supplier)
            .Include(r => r.Details).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (receipt is null) return NotFound();
        return View(receipt);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = await _db.Suppliers.Where(s => s.IsActive).ToListAsync();
        ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
        return View(new StockInForm { Lines = { new StockLineForm() } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockInForm form)
    {
        form.Lines = form.Lines?.Where(l => l.ProductVariantId > 0 && l.Quantity > 0).ToList() ?? new();
        if (form.Lines.Count == 0) ModelState.AddModelError("", "Vui lòng nhập ít nhất một dòng sản phẩm.");

        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = await _db.Suppliers.Where(s => s.IsActive).ToListAsync();
            ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
            return View(form);
        }

        var userId = _userManager.GetUserId(User)!;
        var lines = form.Lines.Select(l => new StockLine(l.ProductVariantId, l.Quantity, l.ImportPrice, l.ExpiryDate, l.BatchCode)).ToList();
        var receipt = await _inventoryService.CreateStockInAsync(form.SupplierId, userId, lines, form.Note);

        TempData["AdminMessage"] = receipt.Status == DocumentStatus.PendingApproval
            ? $"Đã tạo phiếu nhập {receipt.Code} — giá trị lớn, cần Quản lý duyệt trước khi cộng vào tồn kho."
            : $"Đã tạo phiếu nhập {receipt.Code} và cập nhật tồn kho.";
        return RedirectToAction(nameof(Details), new { id = receipt.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerPolicy)]
    public async Task<IActionResult> Approve(int id)
    {
        await _inventoryService.ApproveStockInAsync(id, _userManager.GetUserId(User)!);
        TempData["AdminMessage"] = "Đã duyệt phiếu nhập, tồn kho đã được cập nhật.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var receipt = await _db.StockInReceipts.FindAsync(id);
        if (receipt is null) return NotFound();
        if (receipt.Status != DocumentStatus.PendingApproval)
        {
            TempData["AdminMessage"] = "Chỉ hủy được phiếu đang chờ duyệt.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.StockInReceipts.Remove(receipt); // chưa duyệt nên chưa tạo InventoryBatch, xóa an toàn
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã hủy phiếu nhập.";
        return RedirectToAction(nameof(Index));
    }
}
