using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.ManagerPolicy)]
public class CustomersController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CustomerGroup? group)
    {
        var query = _db.Users.AsQueryable();
        if (group.HasValue) query = query.Where(u => u.CustomerGroup == group);
        ViewBag.GroupFilter = group;
        return View(await query.OrderByDescending(u => u.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        var orders = await _db.Orders.Where(o => o.UserId == id).OrderByDescending(o => o.CreatedAt).Include(o => o.Details).ToListAsync();
        ViewBag.Orders = orders;
        ViewBag.TotalSpent = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGroupAndNote(string id, CustomerGroup customerGroup, string? careNote)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.CustomerGroup = customerGroup;
        user.CareNote = careNote;
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã cập nhật thông tin khách hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
