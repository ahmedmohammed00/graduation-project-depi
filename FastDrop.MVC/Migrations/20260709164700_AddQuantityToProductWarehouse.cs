using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityToProductWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Product_Warehouse",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Product_Warehouse");
        }
    }
}
