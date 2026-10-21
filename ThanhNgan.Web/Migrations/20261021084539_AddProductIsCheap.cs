using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThanhNgan.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIsCheap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCheap",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCheap",
                table: "Products");
        }
    }
}
