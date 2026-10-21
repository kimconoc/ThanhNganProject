using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Models.Entities;
using ThanhNgan.Web.Models.Identity;
using ThanhNgan.Web.Services;

namespace ThanhNgan.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in RoleNames.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@thanhnganseafood.com.vn";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Quản trị viên",
                CreatedAt = DateTime.UtcNow
            };
            // ponytail: mật khẩu mặc định cho môi trường dev/demo — bắt buộc đổi khi lên production.
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        }

        if (!db.Categories.Any())
        {
            var dongLanh = new Category { Name = "Hải sản đông lạnh", Slug = "hai-san-dong-lanh", DisplayOrder = 1 };
            var tuoiSong = new Category { Name = "Hải sản tươi sống", Slug = "hai-san-tuoi-song", DisplayOrder = 2 };
            var mamCa = new Category { Name = "Mắm cá cơm", Slug = "mam-ca-com", DisplayOrder = 3 };
            db.Categories.AddRange(dongLanh, tuoiSong, mamCa);
            await db.SaveChangesAsync();

            db.Categories.AddRange(
                new Category { Name = "Tôm đông lạnh", Slug = "tom-dong-lanh", ParentCategoryId = dongLanh.Id, DisplayOrder = 1 },
                new Category { Name = "Mực đông lạnh", Slug = "muc-dong-lanh", ParentCategoryId = dongLanh.Id, DisplayOrder = 2 },
                new Category { Name = "Cua ghẹ tươi", Slug = "cua-ghe-tuoi", ParentCategoryId = tuoiSong.Id, DisplayOrder = 1 },
                new Category { Name = "Cá tươi", Slug = "ca-tuoi", ParentCategoryId = tuoiSong.Id, DisplayOrder = 2 });
            await db.SaveChangesAsync();

            var sampleProduct = new Product
            {
                Name = "Tôm sú đông lạnh",
                Slug = "tom-su-dong-lanh",
                CategoryId = dongLanh.Id,
                Description = "Tôm sú size lớn, đông lạnh nguyên con, đánh bắt tự nhiên.",
                Origin = "Cà Mau",
                PreservationInstructions = "Bảo quản ở -18°C, dùng trong 6 tháng.",
                IsFeatured = true,
                IsNew = true
            };
            db.Products.Add(sampleProduct);
            await db.SaveChangesAsync();

            var variant500g = new ProductVariant { ProductId = sampleProduct.Id, PackagingSpec = "500g", Sku = "TSDL-500G", Price = 180000 };
            var variant1kg = new ProductVariant { ProductId = sampleProduct.Id, PackagingSpec = "1kg", Sku = "TSDL-1KG", Price = 340000 };
            db.ProductVariants.AddRange(variant500g, variant1kg);

            db.ShippingMethods.AddRange(
                new ShippingMethod { Name = "Giao nội thành", Type = ShippingType.Local, Fee = 25000 },
                new ShippingMethod { Name = "Giao tỉnh (xe lạnh)", Type = ShippingType.ColdProvince, Fee = 45000 },
                new ShippingMethod { Name = "Nhận tại cửa hàng", Type = ShippingType.StorePickup, Fee = 0 });

            var supplier = new Supplier { Name = "Vựa hải sản Cà Mau", ContactName = "Anh Ba", Phone = "0909000000" };
            db.Suppliers.Add(supplier);

            await db.SaveChangesAsync();

            // Tạo tồn kho demo qua phiếu nhập thật (InventoryService) thay vì gán thẳng StockQuantity,
            // để có InventoryBatch làm nguồn cho xuất kho FIFO sau này.
            var inventoryService = services.GetRequiredService<InventoryService>();
            var seedReceipt = await inventoryService.CreateStockInAsync(supplier.Id, "system-seed", new List<StockLine>
            {
                new StockLine(variant500g.Id, 100, 120000),
                new StockLine(variant1kg.Id, 60, 230000)
            }, note: "Nhập kho khởi tạo dữ liệu demo");

            // Giá trị lô demo vượt ngưỡng duyệt (InventoryService.ApprovalThreshold) -> tự duyệt ngay cho dữ liệu mẫu.
            if (seedReceipt.Status == DocumentStatus.PendingApproval)
                await inventoryService.ApproveStockInAsync(seedReceipt.Id, "system-seed");
        }
    }
}
