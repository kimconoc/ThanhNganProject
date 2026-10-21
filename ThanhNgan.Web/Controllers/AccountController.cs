using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;

namespace ThanhNgan.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    public async Task<IActionResult> OrderHistory()
    {
        var orders = await _db.Orders.Where(o => o.UserId == CurrentUserId)
            .OrderByDescending(o => o.CreatedAt)
            .Include(o => o.Details)
            .ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _db.Orders.Include(o => o.Details).Include(o => o.ShippingMethod)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == CurrentUserId);
        if (order is null) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> Addresses()
    {
        var addresses = await _db.Addresses.Where(a => a.UserId == CurrentUserId).ToListAsync();
        return View(addresses);
    }

    private async Task ClearOtherDefaultsAsync() =>
        await _db.Addresses.Where(a => a.UserId == CurrentUserId).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(Address model)
    {
        model.UserId = CurrentUserId;
        if (model.Id == 0)
        {
            if (model.IsDefault) await ClearOtherDefaultsAsync();
            _db.Addresses.Add(model);
        }
        else
        {
            var existing = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == CurrentUserId);
            if (existing is null) return NotFound();
            if (model.IsDefault && !existing.IsDefault) await ClearOtherDefaultsAsync();
            existing.RecipientName = model.RecipientName;
            existing.Phone = model.Phone;
            existing.Province = model.Province;
            existing.District = model.District;
            existing.Ward = model.Ward;
            existing.DetailAddress = model.DetailAddress;
            existing.IsDefault = model.IsDefault;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == CurrentUserId);
        if (address is null) return NotFound();
        await ClearOtherDefaultsAsync();
        address.IsDefault = true;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == CurrentUserId);
        if (address is not null)
        {
            _db.Addresses.Remove(address);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Addresses));
    }

    public async Task<IActionResult> Wishlist()
    {
        var items = await _db.Wishlists.Where(w => w.UserId == CurrentUserId)
            .Include(w => w.Product).ThenInclude(p => p.Images)
            .Include(w => w.Product).ThenInclude(p => p.Variants)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWishlist(int productId)
    {
        var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == CurrentUserId && w.ProductId == productId);
        bool added;
        if (existing is not null) { _db.Wishlists.Remove(existing); added = false; }
        else { _db.Wishlists.Add(new Wishlist { UserId = CurrentUserId, ProductId = productId }); added = true; }
        await _db.SaveChangesAsync();

        if (Request.Headers.XRequestedWith == "XMLHttpRequest") return Json(new { added });
        return RedirectToAction(nameof(Wishlist));
    }

    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string fullName, string? phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        user!.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        await _userManager.UpdateAsync(user);
        TempData["ProfileSaved"] = true;
        return RedirectToAction(nameof(Profile));
    }
}
