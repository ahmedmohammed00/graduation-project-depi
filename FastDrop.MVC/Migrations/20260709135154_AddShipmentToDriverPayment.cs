using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentToDriverPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShipmentId",
                table: "Driver_Payment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Driver_Payment_ShipmentId",
                table: "Driver_Payment",
                column: "ShipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Driver_Payment_Shipment_ShipmentId",
                table: "Driver_Payment",
                column: "ShipmentId",
                principalTable: "Shipment",
                principalColumn: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Driver_Payment_Shipment_ShipmentId",
                table: "Driver_Payment");

            migrationBuilder.DropIndex(
                name: "IX_Driver_Payment_ShipmentId",
                table: "Driver_Payment");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "Driver_Payment");
        }
    }
}
