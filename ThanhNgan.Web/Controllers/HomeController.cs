using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models;
using ThanhNgan.Web.Models.ViewModels;

namespace ThanhNgan.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        IQueryable<Models.Entities.Product> baseQuery = _db.Products
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .Include(p => p.Variants);

        var bestSellerProductIds = await _db.OrderDetails
            .Where(d => d.Order.Status != Models.Entities.OrderStatus.Cancelled)
            .GroupBy(d => d.ProductVariant.ProductId)
            .OrderByDescending(g => g.Sum(d => d.Quantity))
            .Select(g => g.Key)
            .Take(8)
            .ToListAsync();
        var bestSellerProducts = await baseQuery.Where(p => bestSellerProductIds.Contains(p.Id)).ToListAsync();

        var vm = new HomeViewModel
        {
            TopCategories = await _db.Categories.Where(c => c.ParentCategoryId == null).OrderBy(c => c.DisplayOrder).ToListAsync(),
            BestSellers = bestSellerProductIds.Select(id => bestSellerProducts.FirstOrDefault(p => p.Id == id)).Where(p => p != null).Select(p => p!.ToCard()).ToList(),
            FeaturedProducts = (await baseQuery.Where(p => p.IsFeatured).Take(8).ToListAsync()).Select(p => p.ToCard()).ToList(),
            CheapProducts = (await baseQuery.Where(p => p.IsCheap).Take(8).ToListAsync()).Select(p => p.ToCard()).ToList(),
            NewestProducts = (await baseQuery.OrderByDescending(p => p.CreatedAt).Take(12).ToListAsync()).Select(p => p.ToCard()).ToList(),
            LatestPosts = await _db.BlogPosts.Where(b => b.IsPublished).OrderByDescending(b => b.PublishedAt).Take(3).ToListAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> Contact()
    {
        ViewBag.Contact = await ContactSettings.GetAsync(_db);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(Models.Entities.ContactMessage model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedAt = DateTime.UtcNow;
        _db.ContactMessages.Add(model);
        await _db.SaveChangesAsync();
        TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ, Thanh Ngân sẽ phản hồi sớm nhất!";
        return RedirectToAction(nameof(Contact));
    }

    public async Task<IActionResult> Page(string slug)
    {
        var page = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page is null) return NotFound();
        return View(page);
    }

    public async Task<IActionResult> Faq()
    {
        var items = await _db.FaqItems.OrderBy(f => f.DisplayOrder).ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Blog()
    {
        var posts = await _db.BlogPosts.Where(b => b.IsPublished).OrderByDescending(b => b.PublishedAt).ToListAsync();
        return View(posts);
    }

    public async Task<IActionResult> BlogDetail(string slug)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);
        if (post is null) return NotFound();
        return View(post);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
