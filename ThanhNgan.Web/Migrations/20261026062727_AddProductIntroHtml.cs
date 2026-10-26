using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThanhNgan.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIntroHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntroHtml",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntroHtml",
                table: "Products");
        }
    }
}
