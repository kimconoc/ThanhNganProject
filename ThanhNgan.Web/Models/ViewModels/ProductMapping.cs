using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Models.ViewModels;

public static class ProductMapping
{
    public static ProductCardViewModel ToCard(this Product p)
    {
        var mainImage = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).FirstOrDefault();
        var activeVariants = p.Variants.Where(v => v.IsActive).ToList();
        var cheapest = activeVariants.OrderBy(v => v.EffectivePrice).FirstOrDefault();
        return new ProductCardViewModel
        {
            ProductId = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ImageUrl = mainImage?.ImageUrl,
            IsNew = p.IsNew,
            IsOutOfStock = activeVariants.Count == 0 || activeVariants.All(v => v.StockQuantity <= 0),
            MinPrice = cheapest?.Price ?? 0,
            MinSalePrice = cheapest?.SalePrice.HasValue == true && cheapest.EffectivePrice < cheapest.Price ? cheapest.EffectivePrice : null,
            DefaultVariantId = cheapest?.Id ?? 0
        };
    }
}
