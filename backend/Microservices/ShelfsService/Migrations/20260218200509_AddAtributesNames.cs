using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShelfsService.Migrations
{
    /// <inheritdoc />
    public partial class AddAtributesNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AttributeType",
                schema: "shelf_service",
                table: "AttributesValues",
                newName: "AttributeId");

            migrationBuilder.CreateTable(
                name: "Attributes",
                schema: "shelf_service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeKey = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attributes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributesValues_AttributeId",
                schema: "shelf_service",
                table: "AttributesValues",
                column: "AttributeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributesValues_Attributes_AttributeId",
                schema: "shelf_service",
                table: "AttributesValues",
                column: "AttributeId",
                principalSchema: "shelf_service",
                principalTable: "Attributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributesValues_Attributes_AttributeId",
                schema: "shelf_service",
                table: "AttributesValues");

            migrationBuilder.DropTable(
                name: "Attributes",
                schema: "shelf_service");

            migrationBuilder.DropIndex(
                name: "IX_AttributesValues_AttributeId",
                schema: "shelf_service",
                table: "AttributesValues");

            migrationBuilder.RenameColumn(
                name: "AttributeId",
                schema: "shelf_service",
                table: "AttributesValues",
                newName: "AttributeType");
        }
    }
}
