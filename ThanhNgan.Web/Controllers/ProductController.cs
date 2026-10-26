using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Identity;
using ThanhNgan.Web.Models.ViewModels;

namespace ThanhNgan.Web.Controllers;

public class ProductController : Controller
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromHours(24); // thời hạn khách được sửa đánh giá sau khi gửi

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
            var purchases = await _db.OrderDetails
                .Where(od => od.Order.UserId == userId
                    && od.Order.Status == Models.Entities.OrderStatus.Completed
                    && od.ProductVariant.ProductId == product.Id)
                .OrderByDescending(od => od.Order.CreatedAt)
                .Select(od => new { od.Id, od.Order.Code, od.Order.CreatedAt, od.PackagingSpec })
                .ToListAsync();
            var ids = purchases.Select(p => p.Id).ToList();
            var mine = await _db.Reviews.Where(r => r.OrderDetailId != null && ids.Contains(r.OrderDetailId.Value)).ToListAsync();
            var now = DateTime.UtcNow;
            vm.Purchases = purchases.Select(p =>
            {
                var review = mine.FirstOrDefault(r => r.OrderDetailId == p.Id);
                return new PurchaseReviewItem
                {
                    OrderDetailId = p.Id,
                    OrderCode = p.Code,
                    OrderDate = p.CreatedAt,
                    PackagingSpec = p.PackagingSpec,
                    Review = review,
                    CanEdit = review is null || now - review.CreatedAt <= EditWindow
                };
            }).ToList();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> AddReview(int orderDetailId, int rating, string? comment)
    {
        var userId = _userManager.GetUserId(User)!;
        var od = await _db.OrderDetails
            .Include(d => d.Order).Include(d => d.ProductVariant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(d => d.Id == orderDetailId);
        if (od is null) return NotFound();
        if (od.Order.UserId != userId || od.Order.Status != Models.Entities.OrderStatus.Completed) return Forbid();

        var product = od.ProductVariant.Product;
        var detailsUrl = Url.Action(nameof(Details), new { slug = product.Slug }) + "#tab-reviews";

        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.OrderDetailId == orderDetailId);
        if (review is null)
        {
            _db.Reviews.Add(new Models.Entities.Review
            {
                ProductId = product.Id,
                UserId = userId,
                OrderDetailId = orderDetailId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment
            });
            TempData["ReviewMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
        }
        else
        {
            if (DateTime.UtcNow - review.CreatedAt > EditWindow)
            {
                TempData["ReviewMessage"] = "Đã quá 24 giờ kể từ lúc đánh giá, bạn không thể chỉnh sửa nữa.";
                return Redirect(detailsUrl);
            }
            review.Rating = Math.Clamp(rating, 1, 5);
            review.Comment = comment;
            TempData["ReviewMessage"] = "Đã cập nhật đánh giá của bạn.";
        }
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Gửi trùng gần như đồng thời: unique index trên OrderDetailId đã chặn bản thứ hai.
            TempData["ReviewMessage"] = "Đánh giá của bạn đã được ghi nhận.";
        }
        return Redirect(detailsUrl);
    }
}
