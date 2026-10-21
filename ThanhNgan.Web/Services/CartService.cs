using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Services;

public interface ICartService
{
    /// <summary>Lấy giỏ hàng hiện tại (theo UserId nếu đã đăng nhập, hoặc cookie session nếu khách vãng lai), tự merge giỏ khách vào giỏ user khi vừa đăng nhập.</summary>
    Task<Cart> GetOrCreateCartAsync(HttpContext httpContext);
    Task AddItemAsync(HttpContext httpContext, int productVariantId, int quantity);
    Task UpdateQuantityAsync(HttpContext httpContext, int cartItemId, int quantity);
    Task RemoveItemAsync(HttpContext httpContext, int cartItemId);
    Task<int> GetItemCountAsync(HttpContext httpContext);
}

public class CartService : ICartService
{
    private const string CartCookieName = "tn_cart_sid";
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db) => _db = db;

    private const string CartSessionIdItemsKey = "tn_cart_sid_resolved";

    // Cache theo HttpContext.Items (phạm vi 1 request): nếu gọi GetOrSetSessionId nhiều lần trong cùng
    // request (vd AddItemAsync rồi GetItemCountAsync) mà cookie chưa tồn tại, Request.Cookies sẽ chưa thấy
    // cookie vừa ghi vào Response.Cookies ở lần gọi trước -> sinh sid mới -> tạo nhầm giỏ hàng thứ 2.
    private string GetOrSetSessionId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(CartSessionIdItemsKey, out var cached) && cached is string cachedSid)
            return cachedSid;

        if (httpContext.Request.Cookies.TryGetValue(CartCookieName, out var sid) && !string.IsNullOrEmpty(sid))
        {
            httpContext.Items[CartSessionIdItemsKey] = sid;
            return sid;
        }

        sid = Guid.NewGuid().ToString("N");
        httpContext.Response.Cookies.Append(CartCookieName, sid, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(90),
            HttpOnly = true,
            IsEssential = true
        });
        httpContext.Items[CartSessionIdItemsKey] = sid;
        return sid;
    }

    public async Task<Cart> GetOrCreateCartAsync(HttpContext httpContext)
    {
        var userId = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;
        var sessionId = GetOrSetSessionId(httpContext);

        IQueryable<Cart> query = _db.Carts.Include(c => c.Items).ThenInclude(i => i.ProductVariant).ThenInclude(v => v.Product);

        if (userId is not null)
        {
            var userCart = await query.FirstOrDefaultAsync(c => c.UserId == userId);
            var guestCart = await query.FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);

            if (guestCart is not null && guestCart.Items.Count > 0)
            {
                userCart ??= await CreateCartAsync(userId: userId, sessionId: null);
                foreach (var item in guestCart.Items)
                {
                    var existing = userCart.Items.FirstOrDefault(i => i.ProductVariantId == item.ProductVariantId);
                    if (existing is not null) existing.Quantity += item.Quantity;
                    else userCart.Items.Add(new CartItem { ProductVariantId = item.ProductVariantId, Quantity = item.Quantity, CartId = userCart.Id });
                }
                _db.Carts.Remove(guestCart);
                await _db.SaveChangesAsync();
            }

            return userCart ?? await CreateCartAsync(userId: userId, sessionId: null);
        }

        var cart = await query.FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);
        return cart ?? await CreateCartAsync(userId: null, sessionId: sessionId);
    }

    private async Task<Cart> CreateCartAsync(string? userId, string? sessionId)
    {
        var cart = new Cart { UserId = userId, SessionId = sessionId };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        cart.Items = new List<CartItem>();
        return cart;
    }

    public async Task AddItemAsync(HttpContext httpContext, int productVariantId, int quantity)
    {
        if (quantity <= 0) return;
        var cart = await GetOrCreateCartAsync(httpContext);
        var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == productVariantId);
        if (item is null)
            _db.CartItems.Add(new CartItem { CartId = cart.Id, ProductVariantId = productVariantId, Quantity = quantity });
        else
            item.Quantity += quantity;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateQuantityAsync(HttpContext httpContext, int cartItemId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(httpContext);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return;
        if (quantity <= 0) _db.CartItems.Remove(item);
        else item.Quantity = quantity;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(HttpContext httpContext, int cartItemId)
    {
        var cart = await GetOrCreateCartAsync(httpContext);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetItemCountAsync(HttpContext httpContext)
    {
        var cart = await GetOrCreateCartAsync(httpContext);
        return cart.Items.Sum(i => i.Quantity);
    }
}
