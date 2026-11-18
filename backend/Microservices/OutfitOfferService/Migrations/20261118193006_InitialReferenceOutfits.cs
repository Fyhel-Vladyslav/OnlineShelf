using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutfitOfferService.Migrations
{
    /// <inheritdoc />
    public partial class InitialReferenceOutfits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "outfit_offer_service");

            migrationBuilder.CreateTable(
                name: "ReferenceBatches",
                schema: "outfit_offer_service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferencePhotos",
                schema: "outfit_offer_service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    BigImage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SmallImage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferencePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferencePhotos_ReferenceBatches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "outfit_offer_service",
                        principalTable: "ReferenceBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceOutfits",
                schema: "outfit_offer_service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsUsedForTraining = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceOutfits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceOutfits_ReferencePhotos_PhotoId",
                        column: x => x.PhotoId,
                        principalSchema: "outfit_offer_service",
                        principalTable: "ReferencePhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceItems",
                schema: "outfit_offer_service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: true),
                    AttributeSeason = table.Column<int>(type: "integer", nullable: true),
                    AttributePattern = table.Column<int>(type: "integer", nullable: true),
                    AttributeMaterial = table.Column<int>(type: "integer", nullable: true),
                    ColorMain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ColorSecond = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    BoxX1 = table.Column<int>(type: "integer", nullable: false),
                    BoxY1 = table.Column<int>(type: "integer", nullable: false),
                    BoxX2 = table.Column<int>(type: "integer", nullable: false),
                    BoxY2 = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceItems_ReferenceOutfits_OutfitId",
                        column: x => x.OutfitId,
                        principalSchema: "outfit_offer_service",
                        principalTable: "ReferenceOutfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceBatches_OwnerUserId",
                schema: "outfit_offer_service",
                table: "ReferenceBatches",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceItems_OutfitId",
                schema: "outfit_offer_service",
                table: "ReferenceItems",
                column: "OutfitId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceOutfits_PhotoId",
                schema: "outfit_offer_service",
                table: "ReferenceOutfits",
                column: "PhotoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceOutfits_Status",
                schema: "outfit_offer_service",
                table: "ReferenceOutfits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReferencePhotos_BatchId",
                schema: "outfit_offer_service",
                table: "ReferencePhotos",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferencePhotos_OwnerUserId_ContentHash",
                schema: "outfit_offer_service",
                table: "ReferencePhotos",
                columns: new[] { "OwnerUserId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferencePhotos_Status_NextAttemptAt",
                schema: "outfit_offer_service",
                table: "ReferencePhotos",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceItems",
                schema: "outfit_offer_service");

            migrationBuilder.DropTable(
                name: "ReferenceOutfits",
                schema: "outfit_offer_service");

            migrationBuilder.DropTable(
                name: "ReferencePhotos",
                schema: "outfit_offer_service");

            migrationBuilder.DropTable(
                name: "ReferenceBatches",
                schema: "outfit_offer_service");
        }
    }
}
