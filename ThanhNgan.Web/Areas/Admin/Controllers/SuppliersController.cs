using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.WarehousePolicy)]
public class SuppliersController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public SuppliersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Suppliers.ToListAsync());

    public IActionResult Create() => View(new Supplier());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Suppliers.Add(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã thêm nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return NotFound();
        return View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Supplier model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        _db.Suppliers.Update(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã cập nhật nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }
}
