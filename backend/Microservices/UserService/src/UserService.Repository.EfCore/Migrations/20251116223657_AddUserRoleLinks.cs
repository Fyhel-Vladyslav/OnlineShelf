using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "user-service",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserRoleLink",
                schema: "user-service",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleLink", x => new { x.UserId, x.Role });
                    table.ForeignKey(
                        name: "FK_UserRoleLink_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "user-service",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEHkSP0s7Jv4tRM3HgLxrhmEkgwlxzDn+XHbbZjIVWUnGYIZWVEY5Zgmh0gpl6JAF4Q==");

            migrationBuilder.UpdateData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEI4cRC2MXQNNcCJCh2m1w+g452KVBfKKuXgrlc9nn8HCk64K8egBH3B8iRp0XSw5IQ==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoleLink",
                schema: "user-service");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "user-service",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2c9d6f42-8b54-4a02-b0a5-800426cd7581"),
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "hashed_password_2", 5 });

            migrationBuilder.UpdateData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "hashed_password_1", 5 });
        }
    }
}
