using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class InitDataAdding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "user-service",
                table: "Users",
                columns: new[] { "Id", "Avatar", "DateCreated", "Email", "EmailVerified", "Login", "PasswordHash", "State", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"), null, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc), "test@example.com", false, "test", "hashed_password_2", 0, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc) },
                    { new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"), null, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc), "admin@example.com", false, "admin", "hashed_password_1", 0, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"));

            migrationBuilder.DeleteData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"));
        }
    }
}
