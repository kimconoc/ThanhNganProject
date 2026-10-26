using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;

namespace ThanhNgan.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<StockInReceipt> StockInReceipts => Set<StockInReceipt>();
    public DbSet<StockInReceiptDetail> StockInReceiptDetails => Set<StockInReceiptDetail>();
    public DbSet<StockOutReceipt> StockOutReceipts => Set<StockOutReceipt>();
    public DbSet<StockOutReceiptDetail> StockOutReceiptDetails => Set<StockOutReceiptDetail>();
    public DbSet<StockOutAllocation> StockOutAllocations => Set<StockOutAllocation>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentDetail> StockAdjustmentDetails => Set<StockAdjustmentDetail>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductVariant>()
            .Property(v => v.Price).HasColumnType("decimal(18,2)");
        builder.Entity<ProductVariant>()
            .Property(v => v.SalePrice).HasColumnType("decimal(18,2)");
        builder.Entity<ProductVariant>()
            .HasIndex(v => v.Sku).IsUnique();

        builder.Entity<InventoryBatch>()
            .Property(b => b.ImportPrice).HasColumnType("decimal(18,2)");
        builder.Entity<StockInReceiptDetail>()
            .Property(d => d.ImportPrice).HasColumnType("decimal(18,2)");
        builder.Entity<StockOutReceiptDetail>()
            .Property(d => d.UnitCost).HasColumnType("decimal(18,2)");
        builder.Entity<StockOutAllocation>()
            .Property(a => a.UnitCost).HasColumnType("decimal(18,2)");

        builder.Entity<StockOutReceipt>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var name in new[] { "ShippingFee", "SubTotal", "DiscountAmount", "TotalAmount" })
            builder.Entity<Order>().Property(name).HasColumnType("decimal(18,2)");
        builder.Entity<OrderDetail>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Entity<OrderDetail>().Property(d => d.LineTotal).HasColumnType("decimal(18,2)");
        builder.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Entity<ShippingMethod>().Property(s => s.Fee).HasColumnType("decimal(18,2)");
        builder.Entity<Coupon>().Property(c => c.Value).HasColumnType("decimal(18,2)");
        builder.Entity<Coupon>().Property(c => c.MinOrderAmount).HasColumnType("decimal(18,2)");
        builder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();

        builder.Entity<Order>().HasIndex(o => o.Code).IsUnique();
        builder.Entity<StockInReceipt>().HasIndex(r => r.Code).IsUnique();
        builder.Entity<StockOutReceipt>().HasIndex(r => r.Code).IsUnique();
        builder.Entity<StockAdjustment>().HasIndex(a => a.Code).IsUnique();
        builder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        builder.Entity<BlogPost>().HasIndex(b => b.Slug).IsUnique();
        builder.Entity<StaticPage>().HasIndex(p => p.Slug).IsUnique();
        builder.Entity<SystemSetting>().HasKey(s => s.Key);
        // Mỗi lần mua (dòng đơn) chỉ có 1 đánh giá; lọc NOT NULL để đánh giá cũ chưa gắn đơn không bị vướng.
        builder.Entity<Review>().HasIndex(r => r.OrderDetailId).IsUnique().HasFilter("[OrderDetailId] IS NOT NULL");

        // Guest checkout: Order/Address/Review không ràng buộc FK cứng tới User để tránh lỗi khi UserId null.
        builder.Entity<Order>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Chỉ cho cascade delete trên các quan hệ cha-con thật sự (xóa cha thì xóa luôn con);
        // mọi FK bắt buộc khác trong domain của ta chuyển về Restrict để tránh lỗi
        // "multiple cascade paths" của SQL Server và để không xóa nhầm dữ liệu lịch sử (lô hàng, chứng từ...).
        var allowedCascadeChildren = new HashSet<Type>
        {
            typeof(ProductVariant), typeof(ProductImage),
            typeof(StockInReceiptDetail), typeof(StockOutReceiptDetail), typeof(StockOutAllocation),
            typeof(StockAdjustmentDetail), typeof(OrderDetail), typeof(CartItem)
        };
        foreach (var fk in builder.Model.GetEntityTypes()
                     .Where(e => e.ClrType.Namespace == typeof(Product).Namespace)
                     .SelectMany(e => e.GetForeignKeys())
                     .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade && !allowedCascadeChildren.Contains(fk.DeclaringEntityType.ClrType)))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    // Các entity nghiệp vụ cần audit trail đầy đủ (ai tạo/sửa, nội dung thay đổi) theo mục 3.2 của spec.
    private static readonly HashSet<Type> AuditedTypes = new()
    {
        typeof(StockInReceipt), typeof(StockOutReceipt), typeof(StockAdjustment),
        typeof(Product), typeof(ProductVariant), typeof(Order)
    };

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var pending = CapturePendingAudits();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        FinalizeAudits(pending);
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var pending = CapturePendingAudits();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await FinalizeAuditsAsync(pending, cancellationToken);
        return result;
    }

    private record PendingAudit(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry, EntityState State, string? Changes, string? KeyValueIfKnown);

    /// <summary>
    /// Phải chụp lại trạng thái TRƯỚC khi gọi SaveChanges: entity Deleted bị detach ngay sau khi lưu nên
    /// không còn đọc được OriginalValue; entity Added thì khóa chính (Id identity) chỉ có sau khi lưu.
    /// </summary>
    private List<PendingAudit> CapturePendingAudits()
    {
        var pending = new List<PendingAudit>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (!AuditedTypes.Contains(entry.Entity.GetType())) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var changes = entry.State == EntityState.Modified
                ? entry.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .ToDictionary(p => p.Metadata.Name, p => new { old = p.OriginalValue, @new = p.CurrentValue })
                : null;

            var keyValue = entry.State == EntityState.Added
                ? null // chưa có Id, đọc lại sau khi save
                : entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";

            pending.Add(new PendingAudit(entry, entry.State, changes is null ? null : JsonSerializer.Serialize(changes), keyValue));
        }
        return pending;
    }

    private List<AuditLog> BuildFinalLogs(List<PendingAudit> pending)
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        var userId = user?.Identity?.IsAuthenticated == true
            ? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;
        var userName = user?.Identity?.IsAuthenticated == true ? user.Identity!.Name ?? "?" : "system";

        return pending.Select(p => new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = p.State.ToString(),
            EntityName = p.Entry.Entity.GetType().Name,
            EntityId = p.KeyValueIfKnown ?? p.Entry.Properties.FirstOrDefault(pr => pr.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "",
            Changes = p.Changes,
            Timestamp = DateTime.UtcNow
        }).ToList();
    }

    private void FinalizeAudits(List<PendingAudit> pending)
    {
        if (pending.Count == 0) return;
        AuditLogs.AddRange(BuildFinalLogs(pending));
        base.SaveChanges(true);
    }

    private async Task FinalizeAuditsAsync(List<PendingAudit> pending, CancellationToken cancellationToken)
    {
        if (pending.Count == 0) return;
        AuditLogs.AddRange(BuildFinalLogs(pending));
        await base.SaveChangesAsync(true, cancellationToken);
    }
}
