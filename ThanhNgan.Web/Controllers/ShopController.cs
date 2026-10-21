using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.ViewModels;

namespace ThanhNgan.Web.Controllers;

public class ShopController : Controller
{
    private readonly ApplicationDbContext _db;
    public ShopController(ApplicationDbContext db) => _db = db;

    // /Shop  hoặc /Shop/{categorySlug}  — danh mục, lọc, tìm kiếm, sắp xếp, phân trang
    public async Task<IActionResult> Index(string? categorySlug, string? keyword, decimal? minPrice, decimal? maxPrice,
        bool onlyPromotion = false, string sort = "newest", int page = 1)
    {
        var vm = new ShopListViewModel
        {
            CategorySlug = categorySlug,
            Keyword = keyword,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            OnlyPromotion = onlyPromotion,
            Sort = sort,
            Page = page < 1 ? 1 : page
        };

        IQueryable<Product> query = _db.Products.Where(p => p.IsActive)
            .Include(p => p.Images).Include(p => p.Variants).Include(p => p.Category);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var category = await _db.Categories.Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Slug == categorySlug);
            if (category is null) return NotFound();

            vm.CategoryName = category.Name;
            vm.SubCategories = category.SubCategories.OrderBy(c => c.DisplayOrder).ToList();
            var categoryIds = new List<int> { category.Id };
            categoryIds.AddRange(category.SubCategories.Select(c => c.Id));
            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p => p.Name.Contains(keyword));

        if (minPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.Price >= minPrice));
        if (maxPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.Price <= maxPrice));
        if (onlyPromotion)
            query = query.Where(p => p.Variants.Any(v => v.SalePrice != null));

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Variants.Min(v => v.Price)),
            "price_desc" => query.OrderByDescending(p => p.Variants.Min(v => v.Price)),
            _ => query.OrderByDescending(p => p.CreatedAt) // newest (mặc định; "bán chạy" cần dữ liệu đơn hàng, bổ sung ở milestone báo cáo)
        };

        vm.TotalItems = await query.CountAsync();
        var products = await query.Skip((vm.Page - 1) * vm.PageSize).Take(vm.PageSize).ToListAsync();
        vm.Products = products.Select(p => p.ToCard()).ToList();

        return View(vm);
    }
}
