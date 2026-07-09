using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "RoleId", "PermissionsDescription", "RoleName" },
                values: new object[,]
                {
                    { 1, "Full access", "SuperAdmin" },
                    { 2, "Manage shipments and drivers", "Dispatcher" },
                    { 3, "General employee", "Employee" }
                });

            migrationBuilder.Sql("UPDATE Employee SET RoleId = 1 WHERE Email IN ('admin@test.com', 'ahmedmohammed227a@gmail.com')");
            migrationBuilder.Sql("UPDATE Employee SET RoleId = 2 WHERE Email IN ('dispatcher@test.com', 'dispbatecher@test.com')");
            migrationBuilder.Sql("UPDATE Employee SET RoleId = 3 WHERE RoleId IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 3);
        }
    }
}
