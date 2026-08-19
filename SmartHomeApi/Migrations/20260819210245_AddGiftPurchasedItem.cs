using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApi.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftPurchasedItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchasedItem",
                table: "Gifts",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchasedItem",
                table: "Gifts");
        }
    }
}
