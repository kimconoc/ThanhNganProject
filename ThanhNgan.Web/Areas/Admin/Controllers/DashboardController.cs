using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Areas.Admin.Models;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    private const int LowStockThreshold = 10;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var completedOrders = _db.Orders.Where(o => o.Status != OrderStatus.Cancelled);

        var vm = new DashboardViewModel
        {
            RevenueToday = await completedOrders.Where(o => o.CreatedAt >= today).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            RevenueThisMonth = await completedOrders.Where(o => o.CreatedAt >= monthStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            OrdersPendingConfirmation = await _db.Orders.CountAsync(o => o.Status == OrderStatus.PendingConfirmation),
            OrdersProcessing = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Processing),
            LowStockVariants = await _db.ProductVariants.Include(v => v.Product)
                .Where(v => v.StockQuantity <= LowStockThreshold)
                .OrderBy(v => v.StockQuantity)
                .Take(10).ToListAsync(),
            ExpiringSoonBatches = await _db.InventoryBatches.Include(b => b.ProductVariant).ThenInclude(v => v.Product)
                .Where(b => b.QuantityRemaining > 0 && b.ExpiryDate != null && b.ExpiryDate <= today.AddDays(7))
                .OrderBy(b => b.ExpiryDate)
                .Take(10).ToListAsync()
        };

        return View(vm);
    }
}
