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
public class StockAdjustmentController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryService _inventoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StockAdjustmentController(ApplicationDbContext db, InventoryService inventoryService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _inventoryService = inventoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.StockAdjustments.Include(a => a.Details).OrderByDescending(a => a.CreatedAt).ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var adjustment = await _db.StockAdjustments.Include(a => a.Details).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (adjustment is null) return NotFound();
        return View(adjustment);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
        return View(new StockAdjustmentForm { Lines = { new StockAdjustmentLineForm() } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockAdjustmentForm form)
    {
        var lines = form.Lines?.Where(l => l.ProductVariantId > 0).ToList() ?? new();
        if (lines.Count == 0) ModelState.AddModelError("", "Vui lòng nhập ít nhất một dòng sản phẩm.");
        if (!ModelState.IsValid)
        {
            ViewBag.Variants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
            return View(form);
        }

        var variantIds = lines.Select(l => l.ProductVariantId).ToList();
        var systemQuantities = await _db.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.StockQuantity);

        var adjustment = new StockAdjustment { Code = $"KK{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}", CreatedByUserId = _userManager.GetUserId(User)!, Note = form.Note };
        foreach (var line in lines)
            adjustment.Details.Add(new StockAdjustmentDetail
            {
                ProductVariantId = line.ProductVariantId,
                SystemQuantity = systemQuantities.GetValueOrDefault(line.ProductVariantId),
                ActualQuantity = line.ActualQuantity
            });

        _db.StockAdjustments.Add(adjustment);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = $"Đã tạo phiếu kiểm kê {adjustment.Code}, chờ Quản lý duyệt để điều chỉnh tồn kho.";
        return RedirectToAction(nameof(Details), new { id = adjustment.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.ManagerPolicy)]
    public async Task<IActionResult> Approve(int id)
    {
        await _inventoryService.ApproveAdjustmentAsync(id, _userManager.GetUserId(User)!);
        TempData["AdminMessage"] = "Đã duyệt phiếu kiểm kê, tồn kho đã được điều chỉnh.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var adjustment = await _db.StockAdjustments.FindAsync(id);
        if (adjustment is null) return NotFound();
        if (adjustment.Status != DocumentStatus.PendingApproval)
        {
            TempData["AdminMessage"] = "Chỉ hủy được phiếu đang chờ duyệt.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.StockAdjustments.Remove(adjustment); // chưa duyệt nên chưa đụng tồn kho, xóa an toàn
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã hủy phiếu kiểm kê.";
        return RedirectToAction(nameof(Index));
    }
}
