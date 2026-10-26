using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.ManagerPolicy)]
public class ProductsController : AdminControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private static readonly Ganss.Xss.HtmlSanitizer Sanitizer = new();

    public ProductsController(ApplicationDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEditorImage(IFormFile file)
    {
        var ext = file is null ? "" : Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file is null || file.Length == 0 || !AllowedImageExtensions.Contains(ext)) return BadRequest();
        var folder = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{ext}";
        using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            await file.CopyToAsync(stream);
        return Json(new { location = $"/images/products/{fileName}" });
    }

    public async Task<IActionResult> Index(string? keyword)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.Variants).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(p => p.Name.Contains(keyword));
        ViewBag.Keyword = keyword;
        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _db.Categories.ToListAsync();
        return View(new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(model);
        }
        if (model.IntroHtml is not null) model.IntroHtml = Sanitizer.Sanitize(model.IntroHtml);
        _db.Products.Add(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã tạo sản phẩm. Thêm biến thể (quy cách) và hình ảnh bên dưới.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.Include(p => p.Variants).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        ViewBag.Categories = await _db.Categories.ToListAsync();
        var variantIds = product.Variants.Select(v => v.Id).ToList();
        var withHistory = await _db.InventoryBatches.Where(b => variantIds.Contains(b.ProductVariantId)).Select(b => b.ProductVariantId)
            .Union(_db.OrderDetails.Where(d => variantIds.Contains(d.ProductVariantId)).Select(d => d.ProductVariantId))
            .ToListAsync();
        ViewBag.VariantsWithHistory = withHistory.ToHashSet();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(model);
        }
        if (model.IntroHtml is not null) model.IntroHtml = Sanitizer.Sanitize(model.IntroHtml);
        _db.Products.Update(model);
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(int productId, string packagingSpec, string sku, decimal price, decimal? salePrice)
    {
        // Chưa có giá bán (price <= 0) -> tạo ở dạng nháp (ẩn), chưa nhập kho/chưa định giá thì không bán được.
        _db.ProductVariants.Add(new ProductVariant
        {
            ProductId = productId,
            PackagingSpec = packagingSpec,
            Sku = sku,
            Price = price,
            SalePrice = salePrice,
            IsActive = price > 0
        });
        await _db.SaveChangesAsync();
        TempData["AdminMessage"] = price > 0 ? "Đã thêm biến thể." : "Đã thêm biến thể (ở dạng ẩn) — nhập giá bán rồi bấm \"Hiện\" để bắt đầu bán.";
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVariant(int id, int productId, string packagingSpec, string sku, decimal price, decimal? salePrice)
    {
        var variant = await _db.ProductVariants.FindAsync(id);
        if (variant is not null)
        {
            variant.PackagingSpec = packagingSpec;
            variant.Sku = sku;
            variant.Price = price;
            variant.SalePrice = salePrice;
            try
            {
                await _db.SaveChangesAsync();
                TempData["AdminMessage"] = "Đã cập nhật biến thể.";
            }
            catch (DbUpdateException)
            {
                TempData["AdminMessage"] = $"Không lưu được: SKU \"{sku}\" đã tồn tại ở biến thể khác.";
            }
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int id, int productId)
    {
        var variant = await _db.ProductVariants.FindAsync(id);
        if (variant is not null)
        {
            var hasHistory = await _db.InventoryBatches.AnyAsync(b => b.ProductVariantId == id)
                || await _db.OrderDetails.AnyAsync(d => d.ProductVariantId == id);
            if (hasHistory)
                TempData["AdminMessage"] = "Không thể xóa: biến thể đã có lịch sử nhập/xuất hoặc đơn hàng. Dùng nút Ẩn để ngừng bán thay thế.";
            else
            {
                _db.ProductVariants.Remove(variant);
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVariantActive(int id, int productId)
    {
        var variant = await _db.ProductVariants.FindAsync(id);
        if (variant is not null)
        {
            if (!variant.IsActive && variant.Price <= 0)
                TempData["AdminMessage"] = "Cần nhập giá bán (> 0) trước khi hiện bán biến thể này.";
            else
            {
                variant.IsActive = !variant.IsActive;
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImage(int productId, IFormFile imageFile, bool isMain)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            TempData["AdminMessage"] = "Vui lòng chọn file ảnh.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
        var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            TempData["AdminMessage"] = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .webp, .gif).";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        var folder = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{ext}";
        using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            await imageFile.CopyToAsync(stream);
        var imageUrl = $"/images/products/{fileName}";

        if (isMain)
            await _db.ProductImages.Where(i => i.ProductId == productId).ExecuteUpdateAsync(s => s.SetProperty(i => i.IsMain, false));
        _db.ProductImages.Add(new ProductImage { ProductId = productId, ImageUrl = imageUrl, IsMain = isMain });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(int id, int productId)
    {
        if (await _db.ProductImages.AnyAsync(i => i.Id == id && i.ProductId == productId))
        {
            await _db.ProductImages.Where(i => i.ProductId == productId).ExecuteUpdateAsync(s => s.SetProperty(i => i.IsMain, i => i.Id == id));
            TempData["AdminMessage"] = "Đã đổi ảnh chính.";
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int productId)
    {
        var image = await _db.ProductImages.FindAsync(id);
        if (image is not null) { _db.ProductImages.Remove(image); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }
}
