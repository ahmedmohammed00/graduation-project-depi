using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastDrop.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatusToDriverPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Driver_Payment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Driver_Payment");
        }
    }
}
