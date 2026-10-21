using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Services;
using Xunit;

namespace ThanhNgan.Tests;

public class InventoryServiceFifoTests
{
    private static ApplicationDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(ApplicationDbContext db, int variantId, int supplierId)> SeedProductAsync()
    {
        var db = NewDb();
        var category = new Category { Name = "Đông lạnh", Slug = "dong-lanh" };
        db.Categories.Add(category);
        var product = new Product { Name = "Tôm sú", Slug = "tom-su", Category = category };
        db.Products.Add(product);
        var variant = new ProductVariant { Product = product, PackagingSpec = "1kg", Sku = "TS-1KG", Price = 300000 };
        db.ProductVariants.Add(variant);
        var supplier = new Supplier { Name = "Vựa A" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return (db, variant.Id, supplier.Id);
    }

    [Fact]
    public async Task StockOut_ConsumesOldestBatchFirst_AndComputesWeightedAverageCost()
    {
        var (db, variantId, supplierId) = await SeedProductAsync();
        var service = new InventoryService(db);

        // Lô 1: nhập trước, giá rẻ hơn (mùa vụ khác nhau)
        var batch1 = await service.CreateStockInAsync(supplierId, "tester", new List<StockLine>
        {
            new StockLine(variantId, Quantity: 10, ImportPrice: 100_000)
        }, note: "Lô 1");

        // Lô 2: nhập sau, giá cao hơn
        var batch2 = await service.CreateStockInAsync(supplierId, "tester", new List<StockLine>
        {
            new StockLine(variantId, Quantity: 10, ImportPrice: 150_000)
        }, note: "Lô 2");

        Assert.Equal(DocumentStatus.Approved, batch1.Status);
        Assert.Equal(DocumentStatus.Approved, batch2.Status);

        var variant = await db.ProductVariants.FindAsync(variantId);
        Assert.Equal(20, variant!.StockQuantity);

        // Xuất bán 15: phải lấy hết 10 của lô 1 (giá 100k) + 5 của lô 2 (giá 150k) => giá vốn bình quân = (10*100k + 5*150k)/15
        var stockOut = await service.CreateStockOutAsync(StockOutType.Sale, "tester", new List<StockLine>
        {
            new StockLine(variantId, Quantity: 15)
        });

        var detail = await db.StockOutReceiptDetails.Include(d => d.Allocations)
            .FirstAsync(d => d.StockOutReceiptId == stockOut.Id);

        Assert.Equal(2, detail.Allocations.Count); // phải trải trên 2 lô
        var expectedUnitCost = (10 * 100_000m + 5 * 150_000m) / 15m;
        Assert.Equal(expectedUnitCost, detail.UnitCost);

        variant = await db.ProductVariants.FindAsync(variantId);
        Assert.Equal(5, variant!.StockQuantity); // 20 - 15 còn lại, nằm ở lô 2 (giá 150k)

        var remainingBatch = await db.InventoryBatches.Where(b => b.QuantityRemaining > 0).ToListAsync();
        var onlyBatch = Assert.Single(remainingBatch);
        Assert.Equal(150_000m, onlyBatch.ImportPrice); // phần còn lại đúng là của lô nhập sau (lô 1 đã bán hết trước - FIFO)
        Assert.Equal(5, onlyBatch.QuantityRemaining);
    }

    [Fact]
    public async Task StockOut_ThrowsWhenNotEnoughStock()
    {
        var (db, variantId, supplierId) = await SeedProductAsync();
        var service = new InventoryService(db);
        await service.CreateStockInAsync(supplierId, "tester", new List<StockLine> { new StockLine(variantId, 5, 100_000) }, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateStockOutAsync(StockOutType.Sale, "tester", new List<StockLine> { new StockLine(variantId, 10) }));
    }

    [Fact]
    public async Task LargeValueStockIn_RequiresApprovalBeforeAffectingStock()
    {
        var (db, variantId, supplierId) = await SeedProductAsync();
        var service = new InventoryService(db);

        // 100 x 100,000 = 10,000,000 >= ngưỡng 5,000,000 -> phải chờ duyệt, CHƯA cộng tồn kho
        var receipt = await service.CreateStockInAsync(supplierId, "warehouse-staff", new List<StockLine>
        {
            new StockLine(variantId, 100, 100_000)
        }, null);

        Assert.Equal(DocumentStatus.PendingApproval, receipt.Status);
        var variant = await db.ProductVariants.FindAsync(variantId);
        Assert.Equal(0, variant!.StockQuantity);

        await service.ApproveStockInAsync(receipt.Id, "manager");

        variant = await db.ProductVariants.FindAsync(variantId);
        Assert.Equal(100, variant!.StockQuantity);
    }
}
