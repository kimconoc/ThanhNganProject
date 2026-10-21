namespace ThanhNgan.Web.Areas.Admin.Models;

public class RevenuePoint
{
    public string Period { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class CategoryRevenue
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class BestSellerRow
{
    public string ProductName { get; set; } = string.Empty;
    public string PackagingSpec { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class ProfitLossRow
{
    public string OrderCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal Profit => Revenue - Cogs;
}

public class ProfitLossReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ProfitLossRow> Rows { get; set; } = new();
    public decimal TotalRevenue => Rows.Sum(r => r.Revenue);
    public decimal TotalCogs => Rows.Sum(r => r.Cogs);
    public decimal TotalProfit => Rows.Sum(r => r.Profit);
    public decimal ShrinkageValue { get; set; }
    public decimal NetProfit => TotalProfit - ShrinkageValue;
}

public class ShrinkageRow
{
    public DateTime Date { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public string? Note { get; set; }
}
