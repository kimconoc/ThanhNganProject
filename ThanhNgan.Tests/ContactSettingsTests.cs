using Microsoft.EntityFrameworkCore;
using ThanhNgan.Web.Data;
using Xunit;

namespace ThanhNgan.Tests;

public class ContactSettingsTests
{
    private static ApplicationDbContext NewDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetAsync_ReturnsDefaults_WhenNoRowsSaved()
    {
        var db = NewDb();
        var contact = await ContactSettings.GetAsync(db);
        Assert.Equal("1900 0000", contact.Hotline);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTrips()
    {
        var db = NewDb();
        await ContactSettings.SaveAsync(db, "0123456789", "0987654321", "", "a@b.com", " 12 Lý Thường Kiệt, Cà Mau ");
        var contact = await ContactSettings.GetAsync(db);
        Assert.Equal(("0123456789", "0987654321", "", "a@b.com", "12 Lý Thường Kiệt, Cà Mau"), contact);
    }
}
