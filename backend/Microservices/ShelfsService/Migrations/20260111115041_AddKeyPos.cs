using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfsService.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "positionKey",
                schema: "shelf_service",
                table: "Shelfs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "positionKey",
                schema: "shelf_service",
                table: "Items",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "positionKey",
                schema: "shelf_service",
                table: "Shelfs");

            migrationBuilder.DropColumn(
                name: "positionKey",
                schema: "shelf_service",
                table: "Items");
        }
    }
}
