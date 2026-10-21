using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Models.ViewModels;

public class CheckoutViewModel
{
    [BindNever, ValidateNever]
    public Cart Cart { get; set; } = null!;
    public List<ShippingMethod> ShippingMethods { get; set; } = new();

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string RecipientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phương thức giao hàng")]
    public int ShippingMethodId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;

    public string? CouponCode { get; set; }
    public string? Note { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total => SubTotal - DiscountAmount + ShippingFee;
    public string? CouponError { get; set; }
}
