using Microsoft.EntityFrameworkCore;

namespace ThanhNgan.Web.Data;

/// <summary>Đọc/ghi thông tin liên hệ (hotline/zalo/email) lưu trong bảng SystemSettings key-value.</summary>
public static class ContactSettings
{
    public const string HotlineKey = "Contact.Hotline";
    public const string ZaloKey = "Contact.Zalo";
    public const string EmailKey = "Contact.Email";

    public static async Task<(string Hotline, string Zalo, string Email)> GetAsync(ApplicationDbContext db)
    {
        var rows = await db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == HotlineKey || s.Key == ZaloKey || s.Key == EmailKey)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        return (
            rows.GetValueOrDefault(HotlineKey, "1900 0000"),
            rows.GetValueOrDefault(ZaloKey, "0909 000 000"),
            rows.GetValueOrDefault(EmailKey, "cskh@thanhnganseafood.com.vn"));
    }

    public static async Task SaveAsync(ApplicationDbContext db, string hotline, string zalo, string email)
    {
        foreach (var (key, value) in new[] { (HotlineKey, hotline), (ZaloKey, zalo), (EmailKey, email) })
        {
            var existing = await db.SystemSettings.FindAsync(key);
            if (existing is null) db.SystemSettings.Add(new Models.Entities.SystemSetting { Key = key, Value = value });
            else existing.Value = value;
        }
        await db.SaveChangesAsync();
    }
}
