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
    public bool CanReview { get; set; } // đã mua & đăng nhập
}

public class HomeViewModel
{
    public List<Category> TopCategories { get; set; } = new();
    public List<ProductCardViewModel> BestSellers { get; set; } = new();
    public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
    public List<ProductCardViewModel> CheapProducts { get; set; } = new();
    public List<BlogPost> LatestPosts { get; set; } = new();
}
