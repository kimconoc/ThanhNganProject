using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.ManagerPolicy)]
public class SettingsController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public SettingsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.ShippingMethods = await _db.ShippingMethods.ToListAsync();
        ViewBag.StaticPages = await _db.StaticPages.ToListAsync();
        ViewBag.Contact = await ContactSettings.GetAsync(_db);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContact(string hotline, string zalo, string email)
    {
        await ContactSettings.SaveAsync(_db, hotline, zalo, email);
        TempData["AdminMessage"] = "Đã cập nhật thông tin liên hệ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShippingMethod(ShippingMethod model)
    {
        if (model.Id == 0) _db.ShippingMethods.Add(model);
        else _db.ShippingMethods.Update(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã lưu phương thức giao hàng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> EditPage(string slug)
    {
        var page = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug) ?? new StaticPage { Slug = slug, Title = slug };
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPage(StaticPage model)
    {
        var existing = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == model.Slug);
        if (existing is null) _db.StaticPages.Add(model);
        else { existing.Title = model.Title; existing.Content = model.Content; existing.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã lưu nội dung trang.";
        return RedirectToAction(nameof(Index));
    }
}
