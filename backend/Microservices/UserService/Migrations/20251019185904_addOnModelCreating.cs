using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class addOnModelCreating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user-service");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "user-service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "user-service",
                table: "Users",
                columns: new[] { "Id", "DateCreated", "Email", "PasswordHash", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 10, 19, 18, 51, 4, 799, DateTimeKind.Utc).AddTicks(1223), "admin@example.com", "hashed_password_1", "admin" },
                    { 2, new DateTime(2025, 10, 19, 18, 51, 4, 799, DateTimeKind.Utc).AddTicks(2513), "test@example.com", "hashed_password_2", "test" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users",
                schema: "user-service");
        }
    }
}
