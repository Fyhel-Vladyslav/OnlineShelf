using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShelfsService.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAtributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTags",
                schema: "shelf_service");

            migrationBuilder.DropTable(
                name: "TagTypes",
                schema: "shelf_service");

            migrationBuilder.AddColumn<int>(
                name: "AttributeColorMain",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttributeColorSecond",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttributeMatterial",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttributePattern",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttributeSeason",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttributeType",
                schema: "shelf_service",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "isFavorite",
                schema: "shelf_service",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AttributesValues",
                schema: "shelf_service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    AttributeKey = table.Column<int>(type: "integer", nullable: false),
                    AttributeValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributesValues", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributesValues",
                schema: "shelf_service");

            migrationBuilder.DropColumn(
                name: "AttributeColorMain",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AttributeColorSecond",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AttributeMatterial",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AttributePattern",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AttributeSeason",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AttributeType",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "isFavorite",
                schema: "shelf_service",
                table: "Items");

            migrationBuilder.CreateTable(
                name: "TagTypes",
                schema: "shelf_service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemTags",
                schema: "shelf_service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagTypeId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTags_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "shelf_service",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemTags_TagTypes_TagTypeId",
                        column: x => x.TagTypeId,
                        principalSchema: "shelf_service",
                        principalTable: "TagTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemTags_ItemId",
                schema: "shelf_service",
                table: "ItemTags",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemTags_TagTypeId",
                schema: "shelf_service",
                table: "ItemTags",
                column: "TagTypeId");
        }
    }
}
