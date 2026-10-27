using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Models.ViewModels;

public class ProductCardViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsNew { get; set; }
    public bool IsOutOfStock { get; set; }
    public decimal MinPrice { get; set; }
    public decimal? MinSalePrice { get; set; }
    public int DefaultVariantId { get; set; }
}

public class ShopListViewModel
{
    public string? CategorySlug { get; set; }
    public string? CategoryName { get; set; }
    public List<Category> SubCategories { get; set; } = new();
    public List<ProductCardViewModel> Products { get; set; } = new();

    public string? Keyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyPromotion { get; set; }
    public string Sort { get; set; } = "newest"; // newest | price_asc | price_desc | best_selling

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    public List<Product> RelatedProducts { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<PurchaseReviewItem> Purchases { get; set; } = new(); // các lần mua đã hoàn thành của người dùng (mỗi lần mua được đánh giá 1 lần)
    public bool CanReview => Purchases.Count > 0;
}

public class PurchaseReviewItem
{
    public int OrderDetailId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string PackagingSpec { get; set; } = string.Empty;
    public Review? Review { get; set; }
    public bool CanEdit { get; set; } // chưa đánh giá, hoặc còn trong thời hạn sửa
}

public record LicenseFile(string Url, string Title, bool IsImage);

public class AboutViewModel
{
    public List<LicenseFile> Licenses { get; set; } = new();
    public StaticPage? Story { get; set; } // nội dung "gioi-thieu" admin soạn trong Cài đặt (tùy chọn)
    public List<Category> TopCategories { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public int ProductCount { get; set; }
    public int CompletedOrderCount { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
}

public class HomeViewModel
{
    public List<Category> TopCategories { get; set; } = new();
    public List<ProductCardViewModel> BestSellers { get; set; } = new();
    public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
    public List<ProductCardViewModel> CheapProducts { get; set; } = new();
    public List<ProductCardViewModel> NewestProducts { get; set; } = new();
    public List<BlogPost> LatestPosts { get; set; } = new();
}
