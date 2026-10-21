using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Identity;
using ThanhNgan.Web.Models.ViewModels;

namespace ThanhNgan.Web.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Details(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Category)
            .Include(p => p.Reviews.Where(r => r.IsApproved)).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product is null) return NotFound();

        var related = await _db.Products
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
            .Include(p => p.Images).Include(p => p.Variants)
            .Take(4).ToListAsync();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            RelatedProducts = related,
            ReviewCount = product.Reviews.Count,
            AverageRating = product.Reviews.Count > 0 ? product.Reviews.Average(r => r.Rating) : 0
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            vm.CanReview = await _db.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.Order.UserId == userId
                    && od.Order.Status == Models.Entities.OrderStatus.Completed
                    && od.ProductVariant.ProductId == product.Id);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
    {
        var userId = _userManager.GetUserId(User)!;
        var product = await _db.Products.FindAsync(productId);
        if (product is null) return NotFound();

        var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);
        if (!alreadyReviewed)
        {
            _db.Reviews.Add(new Models.Entities.Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment
            });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { slug = product.Slug });
    }
}
