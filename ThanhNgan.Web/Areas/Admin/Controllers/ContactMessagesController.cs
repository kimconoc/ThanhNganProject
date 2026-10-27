using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = $"{RoleNames.SalesStaff},{RoleNames.Manager},{RoleNames.Admin}")]
public class ContactMessagesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public ContactMessagesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(bool showHandled = false)
    {
        ViewBag.ShowHandled = showHandled;
        return View(await _db.ContactMessages.Where(m => showHandled || !m.IsHandled)
            .OrderByDescending(m => m.CreatedAt).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHandled(int id, bool showHandled)
    {
        var msg = await _db.ContactMessages.FindAsync(id);
        if (msg is not null)
        {
            msg.IsHandled = !msg.IsHandled;
            await _db.SaveChangesAsync();
            TempData["AdminMessage"] = msg.IsHandled ? "Đã đánh dấu đã xử lý." : "Đã chuyển về chưa xử lý.";
        }
        return RedirectToAction(nameof(Index), new { showHandled });
    }
}
