using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class InitialSeedFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "UserRoleLinks",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 1, new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580") });

            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"));

            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "user_service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "user_service",
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "System administrator", "Admin" },
                    { 2, "Regular user", "User" },
                    { 3, "Paid premium account", "PremiumUser" },
                    { 4, "Designer role", "Designer" }
                });

            migrationBuilder.InsertData(
                schema: "user_service",
                table: "Users",
                columns: new[] { "Id", "Avatar", "DateCreated", "Email", "EmailVerified", "Login", "PasswordHash", "State", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"), null, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc), "test@example.com", false, "test", "AQAAAAIAAYagAAAAEHkSP0s7Jv4tRM3HgLxrhmEkgwlxzDn+XHbbZjIVWUnGYIZWVEY5Zgmh0gpl6JAF4Q==", 0, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc) },
                    { new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"), null, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc), "admin@example.com", false, "admin", "AQAAAAIAAYagAAAAEI4cRC2MXQNNcCJCh2m1w+g452KVBfKKuXgrlc9nn8HCk64K8egBH3B8iRp0XSw5IQ==", 0, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "user_service",
                table: "UserRoleLinks",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 1, new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580") });
        }
    }
}
