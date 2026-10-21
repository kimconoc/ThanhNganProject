namespace ThanhNgan.Web.Areas.Admin.Models;

public class StockLineForm
{
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchCode { get; set; }
}

public class StockInForm
{
    public int SupplierId { get; set; }
    public string? Note { get; set; }
    public List<StockLineForm> Lines { get; set; } = new();
}

public class StockOutForm
{
    public ThanhNgan.Web.Models.Entities.StockOutType Type { get; set; }
    public string? Note { get; set; }
    public string? TargetWarehouse { get; set; }
    public List<StockLineForm> Lines { get; set; } = new();
}

public class StockAdjustmentLineForm
{
    public int ProductVariantId { get; set; }
    public int ActualQuantity { get; set; }
}

public class StockAdjustmentForm
{
    public string? Note { get; set; }
    public List<StockAdjustmentLineForm> Lines { get; set; } = new();
}
