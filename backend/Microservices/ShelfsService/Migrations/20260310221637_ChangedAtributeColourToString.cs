using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfsService.Migrations
{
    /// <inheritdoc />
    public partial class ChangedAtributeColourToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AttributeColorSecond",
                schema: "shelf_service",
                table: "Items",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "AttributeColorMain",
                schema: "shelf_service",
                table: "Items",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AttributeColorSecond",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "AttributeColorMain",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
