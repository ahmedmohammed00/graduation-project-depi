using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class ProtectSessionsAndDeliveryPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT [ShipmentId] FROM [Driver_Payment]
                    WHERE [ShipmentId] IS NOT NULL GROUP BY [ShipmentId] HAVING COUNT_BIG(*) > 1)
                BEGIN
                    THROW 51001, 'Duplicate shipment payouts require operator review before this migration. No payment records were deleted.', 1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Driver_Payment_ShipmentId",
                table: "Driver_Payment");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryConfirmedAt",
                table: "Shipment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Shipment",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Merchant",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Employee",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.CreateIndex(
                name: "IX_Driver_Payment_ShipmentId",
                table: "Driver_Payment",
                column: "ShipmentId",
                unique: true,
                filter: "[ShipmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Driver_Payment_ShipmentId",
                table: "Driver_Payment");

            migrationBuilder.DropColumn(
                name: "DeliveryConfirmedAt",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Merchant");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Employee");

            migrationBuilder.CreateIndex(
                name: "IX_Driver_Payment_ShipmentId",
                table: "Driver_Payment",
                column: "ShipmentId");
        }
    }
}
