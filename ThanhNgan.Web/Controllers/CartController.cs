using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Services;

namespace ThanhNgan.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly Data.ApplicationDbContext _db;

    public CartController(ICartService cartService, Data.ApplicationDbContext db)
    {
        _cartService = cartService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetOrCreateCartAsync(HttpContext);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int variantId, int quantity = 1, string redirectTo = "cart")
    {
        var variant = await _db.ProductVariants.FindAsync(variantId);
        if (variant is null) return NotFound();

        await _cartService.AddItemAsync(HttpContext, variantId, quantity);
        var count = await _cartService.GetItemCountAsync(HttpContext);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true, cartCount = count });

        TempData["CartMessage"] = $"Đã thêm \"{variant.PackagingSpec}\" vào giỏ hàng.";
        return redirectTo == "checkout" ? RedirectToAction("Index", "Checkout") : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(HttpContext, cartItemId, quantity);
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        await _cartService.RemoveItemAsync(HttpContext, cartItemId);
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        return Json(new { count = await _cartService.GetItemCountAsync(HttpContext) });
    }
}
