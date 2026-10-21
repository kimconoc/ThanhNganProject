using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Areas.Admin.Models;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Controllers;

// Quản lý xem báo cáo ở mức giới hạn, Admin xem toàn bộ — tạm dùng chung quyền Manager/Admin,
// có thể tách policy riêng khi cần giới hạn phạm vi số liệu hiển thị cho Quản lý.
[Authorize(Roles = RoleNames.ManagerPolicy)]
public class ReportsController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    private static (DateTime from, DateTime to) NormalizeRange(DateTime? from, DateTime? to)
    {
        var effectiveTo = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);
        var effectiveFrom = (from ?? effectiveTo.AddDays(-29)).Date;
        return (effectiveFrom, effectiveTo);
    }

    public IActionResult Index() => RedirectToAction(nameof(Revenue));

    public async Task<IActionResult> Revenue(DateTime? from, DateTime? to, string groupBy = "day")
    {
        var (f, t) = NormalizeRange(from, to);
        ViewBag.From = f; ViewBag.To = t; ViewBag.GroupBy = groupBy;

        var orders = await _db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= f && o.CreatedAt <= t)
            .ToListAsync();

        Func<DateTime, string> keySelector = groupBy switch
        {
            "month" => d => $"{d:yyyy}-{d:MM}",
            "quarter" => d => $"{d.Year} Q{(d.Month - 1) / 3 + 1}",
            "year" => d => $"{d.Year}",
            _ => d => d.ToString("dd/MM/yyyy")
        };

        var points = orders.GroupBy(o => keySelector(o.CreatedAt))
            .Select(g => new RevenuePoint { Period = g.Key, Revenue = g.Sum(o => o.TotalAmount), OrderCount = g.Count() })
            .OrderBy(p => p.Period).ToList();

        var categoryRevenue = await _db.OrderDetails
            .Where(d => d.Order.Status != OrderStatus.Cancelled && d.Order.CreatedAt >= f && d.Order.CreatedAt <= t)
            .Include(d => d.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.Category).ThenInclude(c => c!.ParentCategory)
            .ToListAsync();

        var byCategory = categoryRevenue
            .GroupBy(d => d.ProductVariant.Product.Category.ParentCategory?.Name ?? d.ProductVariant.Product.Category.Name)
            .Select(g => new CategoryRevenue { CategoryName = g.Key, Revenue = g.Sum(d => d.LineTotal) })
            .OrderByDescending(c => c.Revenue).ToList();

        ViewBag.CategoryRevenue = byCategory;
        return View(points);
    }

    public async Task<IActionResult> BestSellers(DateTime? from, DateTime? to)
    {
        var (f, t) = NormalizeRange(from, to);
        ViewBag.From = f; ViewBag.To = t;

        var details = await _db.OrderDetails
            .Where(d => d.Order.Status != OrderStatus.Cancelled && d.Order.CreatedAt >= f && d.Order.CreatedAt <= t)
            .ToListAsync();

        var best = details.GroupBy(d => new { d.ProductName, d.PackagingSpec })
            .Select(g => new BestSellerRow { ProductName = g.Key.ProductName, PackagingSpec = g.Key.PackagingSpec, QuantitySold = g.Sum(d => d.Quantity), Revenue = g.Sum(d => d.LineTotal) })
            .OrderByDescending(b => b.QuantitySold).Take(20).ToList();

        var slowMoving = await _db.ProductVariants.Include(v => v.Product)
            .Where(v => v.StockQuantity > 0)
            .OrderByDescending(v => v.StockQuantity)
            .Take(10).ToListAsync();
        ViewBag.SlowMoving = slowMoving;

        return View(best);
    }

    private async Task<ProfitLossReportViewModel> BuildProfitLossAsync(DateTime from, DateTime to)
    {
        var saleDetails = await _db.StockOutReceiptDetails
            .Where(d => d.StockOutReceipt.Type == StockOutType.Sale && d.StockOutReceipt.Status == DocumentStatus.Approved
                && d.StockOutReceipt.Order != null && d.StockOutReceipt.CreatedAt >= from && d.StockOutReceipt.CreatedAt <= to)
            .Include(d => d.StockOutReceipt).ThenInclude(r => r.Order)
            .ToListAsync();

        var rows = saleDetails.GroupBy(d => d.StockOutReceipt.Order!)
            .Select(g => new ProfitLossRow
            {
                OrderCode = g.Key.Code,
                Date = g.Key.CreatedAt,
                Revenue = g.Key.SubTotal - g.Key.DiscountAmount,
                Cogs = g.Sum(d => d.Quantity * d.UnitCost)
            })
            .OrderByDescending(r => r.Date).ToList();

        var shrinkageValue = await _db.StockOutReceiptDetails
            .Where(d => d.StockOutReceipt.Type == StockOutType.Damage && d.StockOutReceipt.Status == DocumentStatus.Approved
                && d.StockOutReceipt.CreatedAt >= from && d.StockOutReceipt.CreatedAt <= to)
            .SumAsync(d => (decimal?)(d.Quantity * d.UnitCost)) ?? 0;

        return new ProfitLossReportViewModel { From = from, To = to, Rows = rows, ShrinkageValue = shrinkageValue };
    }

    public async Task<IActionResult> ProfitLoss(DateTime? from, DateTime? to)
    {
        var (f, t) = NormalizeRange(from, to);
        return View(await BuildProfitLossAsync(f, t));
    }

    public async Task<IActionResult> Shrinkage(DateTime? from, DateTime? to)
    {
        var (f, t) = NormalizeRange(from, to);
        ViewBag.From = f; ViewBag.To = t;

        var rows = await _db.StockOutReceiptDetails
            .Where(d => d.StockOutReceipt.Type == StockOutType.Damage && d.StockOutReceipt.Status == DocumentStatus.Approved
                && d.StockOutReceipt.CreatedAt >= f && d.StockOutReceipt.CreatedAt <= t)
            .Include(d => d.StockOutReceipt)
            .Include(d => d.ProductVariant).ThenInclude(v => v.Product)
            .Select(d => new ShrinkageRow
            {
                Date = d.StockOutReceipt.CreatedAt,
                ReceiptCode = d.StockOutReceipt.Code,
                ProductName = d.ProductVariant.Product.Name + " (" + d.ProductVariant.PackagingSpec + ")",
                Quantity = d.Quantity,
                Value = d.Quantity * d.UnitCost,
                Note = d.StockOutReceipt.Note
            })
            .OrderByDescending(r => r.Date).ToListAsync();

        return View(rows);
    }

    public async Task<IActionResult> ExportProfitLossExcel(DateTime? from, DateTime? to)
    {
        var (f, t) = NormalizeRange(from, to);
        var report = await BuildProfitLossAsync(f, t);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Lời lỗ");
        ws.Cell(1, 1).Value = "Mã đơn"; ws.Cell(1, 2).Value = "Ngày"; ws.Cell(1, 3).Value = "Doanh thu"; ws.Cell(1, 4).Value = "Giá vốn"; ws.Cell(1, 5).Value = "Lợi nhuận";
        var row = 2;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.OrderCode;
            ws.Cell(row, 2).Value = r.Date;
            ws.Cell(row, 3).Value = r.Revenue;
            ws.Cell(row, 4).Value = r.Cogs;
            ws.Cell(row, 5).Value = r.Profit;
            row++;
        }
        ws.Cell(row + 1, 2).Value = "Tổng"; ws.Cell(row + 1, 3).Value = report.TotalRevenue; ws.Cell(row + 1, 4).Value = report.TotalCogs; ws.Cell(row + 1, 5).Value = report.TotalProfit;
        ws.Cell(row + 2, 2).Value = "Hao hụt"; ws.Cell(row + 2, 5).Value = -report.ShrinkageValue;
        ws.Cell(row + 3, 2).Value = "Lợi nhuận thực tế"; ws.Cell(row + 3, 5).Value = report.NetProfit;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoLoiLo_{f:yyyyMMdd}_{t:yyyyMMdd}.xlsx");
    }
}
