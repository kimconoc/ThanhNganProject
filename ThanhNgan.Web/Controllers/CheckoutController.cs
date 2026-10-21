using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;
using ThanhNgan.Web.Models.ViewModels;
using ThanhNgan.Web.Services;
using ThanhNgan.Web.Services.Payments;

namespace ThanhNgan.Web.Controllers;

public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly ICouponService _couponService;
    private readonly INotificationSender _notificationSender;
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(ApplicationDbContext db, ICartService cartService, ICouponService couponService,
        INotificationSender notificationSender, IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cartService = cartService;
        _couponService = couponService;
        _notificationSender = notificationSender;
        _config = config;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetOrCreateCartAsync(HttpContext);
        if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");

        var vm = new CheckoutViewModel
        {
            Cart = cart,
            ShippingMethods = await _db.ShippingMethods.Where(s => s.IsActive).ToListAsync(),
            SubTotal = cart.Items.Sum(i => i.Quantity * i.ProductVariant.EffectivePrice)
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            var defaultAddress = await _db.Addresses.FirstOrDefaultAsync(a => a.UserId == user!.Id && a.IsDefault);
            vm.RecipientName = defaultAddress?.RecipientName ?? user?.FullName ?? "";
            vm.RecipientPhone = defaultAddress?.Phone ?? user?.PhoneNumber ?? "";
            vm.ShippingAddress = defaultAddress is null ? "" : $"{defaultAddress.DetailAddress}, {defaultAddress.Ward}, {defaultAddress.District}, {defaultAddress.Province}";
        }

        vm.ShippingMethodId = vm.ShippingMethods.FirstOrDefault()?.Id ?? 0;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        var cart = await _cartService.GetOrCreateCartAsync(HttpContext);
        var subtotal = cart.Items.Sum(i => i.Quantity * i.ProductVariant.EffectivePrice);
        var result = await _couponService.ApplyAsync(couponCode, subtotal, cart.Items);
        return Json(new { success = result.Coupon != null, discount = result.Discount, error = result.Error });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel form)
    {
        var cart = await _cartService.GetOrCreateCartAsync(HttpContext);
        if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");

        form.ShippingMethods = await _db.ShippingMethods.Where(s => s.IsActive).ToListAsync();
        form.Cart = cart;
        form.SubTotal = cart.Items.Sum(i => i.Quantity * i.ProductVariant.EffectivePrice);

        var shippingMethod = await _db.ShippingMethods.FindAsync(form.ShippingMethodId);
        if (shippingMethod is null) ModelState.AddModelError(nameof(form.ShippingMethodId), "Vui lòng chọn phương thức giao hàng.");

        foreach (var item in cart.Items)
            if (item.Quantity > item.ProductVariant.StockQuantity)
                ModelState.AddModelError("", $"Sản phẩm \"{item.ProductVariant.Product.Name} ({item.ProductVariant.PackagingSpec})\" không đủ hàng tồn kho.");

        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(form.CouponCode))
        {
            var couponResult = await _couponService.ApplyAsync(form.CouponCode, form.SubTotal, cart.Items);
            if (couponResult.Coupon is null) { form.CouponError = couponResult.Error; }
            else { coupon = couponResult.Coupon; form.DiscountAmount = couponResult.Discount; }
        }

        if (!ModelState.IsValid || shippingMethod is null)
        {
            form.ShippingFee = shippingMethod?.Fee ?? 0;
            return View("Index", form);
        }

        var gateway = PaymentGatewayFactory.Resolve(form.PaymentMethod, _config);

        var order = new Order
        {
            Code = $"DH{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}",
            UserId = User.Identity?.IsAuthenticated == true ? _userManager.GetUserId(User) : null,
            RecipientName = form.RecipientName,
            RecipientPhone = form.RecipientPhone,
            ShippingAddress = form.ShippingAddress,
            ShippingMethodId = shippingMethod.Id,
            ShippingFee = shippingMethod.Fee,
            CouponId = coupon?.Id,
            SubTotal = form.SubTotal,
            DiscountAmount = form.DiscountAmount,
            TotalAmount = form.SubTotal - form.DiscountAmount + shippingMethod.Fee,
            PaymentMethod = form.PaymentMethod,
            Note = form.Note,
            Status = OrderStatus.PendingConfirmation
        };

        foreach (var item in cart.Items)
        {
            order.Details.Add(new OrderDetail
            {
                ProductVariantId = item.ProductVariantId,
                ProductName = item.ProductVariant.Product.Name,
                PackagingSpec = item.ProductVariant.PackagingSpec,
                UnitPrice = item.ProductVariant.EffectivePrice,
                Quantity = item.Quantity,
                LineTotal = item.Quantity * item.ProductVariant.EffectivePrice
            });
        }

        _db.Orders.Add(order);

        if (coupon is not null) coupon.UsedCount++;

        await _db.SaveChangesAsync();

        var paymentResult = await gateway.InitiateAsync(order);
        _db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Method = form.PaymentMethod,
            Amount = order.TotalAmount,
            Status = form.PaymentMethod == PaymentMethod.Cod ? PaymentStatus.Pending : PaymentStatus.Pending
        });

        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();

        await _notificationSender.SendOrderConfirmationAsync(order);

        TempData["PaymentMessage"] = paymentResult.Message;
        return RedirectToAction(nameof(Confirmation), new { code = order.Code });
    }

    public async Task<IActionResult> Confirmation(string code)
    {
        var order = await _db.Orders.Include(o => o.Details).Include(o => o.ShippingMethod)
            .FirstOrDefaultAsync(o => o.Code == code);
        if (order is null) return NotFound();
        return View(order);
    }
}
