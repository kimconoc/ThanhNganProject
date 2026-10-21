using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.ManagerPolicy)]
public class CategoriesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories.Include(c => c.ParentCategory).OrderBy(c => c.DisplayOrder).ToListAsync();
        return View(categories);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.ParentCategories = await _db.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();
        return View(new Category());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ParentCategories = await _db.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();
            return View(model);
        }
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã tạo danh mục.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        ViewBag.ParentCategories = await _db.Categories.Where(c => c.ParentCategoryId == null && c.Id != id).ToListAsync();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.ParentCategories = await _db.Categories.Where(c => c.ParentCategoryId == null && c.Id != id).ToListAsync();
            return View(model);
        }
        _db.Categories.Update(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.SubCategories).Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();
        if (category.SubCategories.Count > 0 || category.Products.Count > 0)
        {
            TempData["AdminMessage"] = "Không thể xóa: danh mục còn sản phẩm hoặc danh mục con.";
            return RedirectToAction(nameof(Index));
        }
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã xóa danh mục.";
        return RedirectToAction(nameof(Index));
    }
}
