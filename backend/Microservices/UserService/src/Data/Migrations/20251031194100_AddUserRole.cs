using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                column: "Role",
                value: 5);

            migrationBuilder.UpdateData(
                schema: "user-service",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                column: "Role",
                value: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "user-service",
                table: "Users");
        }
    }
}
