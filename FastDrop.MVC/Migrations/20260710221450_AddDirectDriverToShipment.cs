using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectDriverToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "Shipment",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_DriverId",
                table: "Shipment",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_Driver_DriverId",
                table: "Shipment",
                column: "DriverId",
                principalTable: "Driver",
                principalColumn: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_Driver_DriverId",
                table: "Shipment");

            migrationBuilder.DropIndex(
                name: "IX_Shipment_DriverId",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Shipment");
        }
    }
}
