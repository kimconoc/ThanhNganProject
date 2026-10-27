using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ThanhNgan.Web.Data;

/// <summary>Đọc/ghi thông tin liên hệ (hotline/zalo/messenger/email) lưu trong bảng SystemSettings key-value.</summary>
public static class ContactSettings
{
    public const string HotlineKey = "Contact.Hotline";
    public const string ZaloKey = "Contact.Zalo";
    public const string MessengerKey = "Contact.Messenger";
    public const string EmailKey = "Contact.Email";
    public const string AddressKey = "Contact.Address";

    public static async Task<(string Hotline, string Zalo, string Messenger, string Email, string Address)> GetAsync(ApplicationDbContext db)
    {
        var rows = await db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == HotlineKey || s.Key == ZaloKey || s.Key == MessengerKey || s.Key == EmailKey || s.Key == AddressKey)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        return (
            rows.GetValueOrDefault(HotlineKey, "1900 0000"),
            rows.GetValueOrDefault(ZaloKey, "0909 000 000"),
            rows.GetValueOrDefault(MessengerKey, ""),
            rows.GetValueOrDefault(EmailKey, "cskh@thanhnganseafood.com.vn"),
            rows.GetValueOrDefault(AddressKey, "")); // rỗng thì ẩn dòng địa chỉ, không hiện địa chỉ giả
    }

    public static async Task SaveAsync(ApplicationDbContext db, string hotline, string zalo, string messenger, string email, string? address = null)
    {
        foreach (var (key, value) in new[] { (HotlineKey, hotline), (ZaloKey, zalo), (MessengerKey, messenger ?? ""), (EmailKey, email), (AddressKey, address?.Trim() ?? "") })
        {
            var existing = await db.SystemSettings.FindAsync(key);
            if (existing is null) db.SystemSettings.Add(new Models.Entities.SystemSetting { Key = key, Value = value });
            else existing.Value = value;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>https://zalo.me/{sđt không khoảng trắng/ký tự đặc biệt} để mở chat Zalo trực tiếp.</summary>
    public static string ZaloChatUrl(string zalo) => $"https://zalo.me/{Regex.Replace(zalo ?? "", "[^0-9]", "")}";

    /// <summary>https://m.me/{username hoặc page-id fanpage} để mở chat Messenger trực tiếp.</summary>
    public static string MessengerChatUrl(string messenger) => $"https://m.me/{(messenger ?? "").Trim()}";
}
