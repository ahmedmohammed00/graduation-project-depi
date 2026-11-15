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

            // Role definitions do not approve accounts. Assign roles only through an
            // operator-approved provisioning process using verified employee IDs.
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
