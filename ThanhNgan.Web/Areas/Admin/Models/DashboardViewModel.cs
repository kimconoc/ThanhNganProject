using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Areas.Admin.Models;

public class DashboardViewModel
{
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int OrdersPendingConfirmation { get; set; }
    public int OrdersProcessing { get; set; }
    public List<ProductVariant> LowStockVariants { get; set; } = new();
    public List<InventoryBatch> ExpiringSoonBatches { get; set; } = new();
}
