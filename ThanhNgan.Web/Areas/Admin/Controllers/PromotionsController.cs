using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.ManagerPolicy)]
public class PromotionsController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public PromotionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Coupons.OrderByDescending(c => c.StartDate).ToListAsync());

    public async Task<IActionResult> Create()
    {
        ViewBag.Products = await _db.Products.ToListAsync();
        return View(new Coupon { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(30) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Products = await _db.Products.ToListAsync();
            return View(model);
        }
        _db.Coupons.Add(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã tạo mã khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound();
        ViewBag.Products = await _db.Products.ToListAsync();
        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Coupon model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Products = await _db.Products.ToListAsync();
            return View(model);
        }
        _db.Coupons.Update(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã cập nhật mã khuyến mãi.";
        return RedirectToAction(nameof(Index));
    }
}
