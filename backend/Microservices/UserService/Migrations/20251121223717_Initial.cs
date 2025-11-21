using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user-service");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "user-service",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "user-service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleLinks",
                schema: "user-service",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleLinks", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoleLinks_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "user-service",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleLinks_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "user-service",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "user-service",
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
                schema: "user-service",
                table: "Users",
                columns: new[] { "Id", "Avatar", "DateCreated", "Email", "EmailVerified", "Login", "PasswordHash", "State", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"), null, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc), "test@example.com", false, "test", "AQAAAAIAAYagAAAAEHkSP0s7Jv4tRM3HgLxrhmEkgwlxzDn+XHbbZjIVWUnGYIZWVEY5Zgmh0gpl6JAF4Q==", 0, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc) },
                    { new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"), null, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc), "admin@example.com", false, "admin", "AQAAAAIAAYagAAAAEI4cRC2MXQNNcCJCh2m1w+g452KVBfKKuXgrlc9nn8HCk64K8egBH3B8iRp0XSw5IQ==", 0, new DateTime(2025, 10, 19, 18, 51, 4, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "user-service",
                table: "UserRoleLinks",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 1, new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580") });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleLinks_RoleId",
                schema: "user-service",
                table: "UserRoleLinks",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoleLinks",
                schema: "user-service");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "user-service");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "user-service");
        }
    }
}
