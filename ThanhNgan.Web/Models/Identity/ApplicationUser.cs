using Microsoft.AspNetCore.Identity;
using ThanhNgan.Web.Models.Entities;

namespace ThanhNgan.Web.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public CustomerGroup CustomerGroup { get; set; } = CustomerGroup.Retail;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CareNote { get; set; } // ghi chú CSKH (nội bộ)

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
