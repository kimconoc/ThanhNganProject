using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ThanhNgan.Web.Models.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    // Admin controller nhận trực tiếp entity Product từ form (không qua ViewModel riêng) để gọn code;
    // nav property này không có input tương ứng nên phải loại khỏi bind/validate, tránh lỗi "Category field is required".
    [BindNever, ValidateNever]
    public Category Category { get; set; } = null!;
    public string? Description { get; set; }
    public string? Origin { get; set; } // nguồn gốc xuất xứ
    public string? PreservationInstructions { get; set; } // bảo quản (đông lạnh)
    public string? ExpiryInfo { get; set; } // hạn sử dụng (mắm cá cơm)
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string PackagingSpec { get; set; } = string.Empty; // 500g / 1kg / khay / hũ
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public DateTime? SalePriceStart { get; set; }
    public DateTime? SalePriceEnd { get; set; }
    public int StockQuantity { get; set; } // tồn kho hiện tại (denormalized, nguồn sự thật là tổng InventoryBatch.QuantityRemaining)
    public bool IsActive { get; set; } = true; // ẩn khỏi site mà không xóa — biến thể đã có lịch sử nhập/xuất/đơn hàng không thể xóa cứng

    public decimal EffectivePrice =>
        SalePrice.HasValue
        && (!SalePriceStart.HasValue || SalePriceStart <= DateTime.UtcNow)
        && (!SalePriceEnd.HasValue || SalePriceEnd >= DateTime.UtcNow)
            ? SalePrice.Value
            : Price;

    public ICollection<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsMain { get; set; }
}
