using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThanhNgan.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewOrderDetailUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderDetailId",
                table: "Reviews",
                column: "OrderDetailId",
                unique: true,
                filter: "[OrderDetailId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_OrderDetailId",
                table: "Reviews");
        }
    }
}
