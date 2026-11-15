using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class CaptureShipmentUnitPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Shipment_Product",
                type: "decimal(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Shipment_Product");
        }
    }
}
