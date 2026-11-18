using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfsService.Migrations
{
    /// <inheritdoc />
    public partial class AddItemVisualEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                schema: "shelf_service",
                table: "Items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<float[]>(
                name: "VisualEmbedding",
                schema: "shelf_service",
                table: "Items",
                type: "real[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "VisualEmbedding",
                schema: "shelf_service",
                table: "Items");
        }
    }
}
