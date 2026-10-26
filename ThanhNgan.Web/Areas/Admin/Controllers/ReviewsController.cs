using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.ManagerPolicy)]
public class ReviewsController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReviewsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Reviews.Include(r => r.Product).Include(r => r.User).OrderByDescending(r => r.CreatedAt).ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApproval(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is not null)
        {
            review.IsApproved = !review.IsApproved;
            await _db.SaveChangesAsync();
            TempData["AdminMessage"] = review.IsApproved ? "Đã hiện đánh giá." : "Đã ẩn đánh giá.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is not null)
        {
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            TempData["AdminMessage"] = "Đã xóa đánh giá.";
        }
        return RedirectToAction(nameof(Index));
    }
}
