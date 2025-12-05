using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class ChangedUserSchemeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user_service");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "user-service",
                newName: "Users",
                newSchema: "user_service");

            migrationBuilder.RenameTable(
                name: "UserRoleLinks",
                schema: "user-service",
                newName: "UserRoleLinks",
                newSchema: "user_service");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "user-service",
                newName: "Roles",
                newSchema: "user_service");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user-service");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "user_service",
                newName: "Users",
                newSchema: "user-service");

            migrationBuilder.RenameTable(
                name: "UserRoleLinks",
                schema: "user_service",
                newName: "UserRoleLinks",
                newSchema: "user-service");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "user_service",
                newName: "Roles",
                newSchema: "user-service");
        }
    }
}
