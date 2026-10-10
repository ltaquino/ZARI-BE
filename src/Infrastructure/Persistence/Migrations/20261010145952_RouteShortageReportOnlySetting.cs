using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RouteShortageReportOnlySetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShortageAmount",
                table: "Trips",
                type: "DECIMAL(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RouteShortagePayrollDeductionEnabled",
                table: "Companies",
                type: "tinyint(1)",
                nullable: false,
                // Existing behaviour: shortages can go through payroll until someone turns it off.
                defaultValue: true);

            // Trips closed before this column existed: what was charged is the best record of the shortage.
            migrationBuilder.Sql("UPDATE Trips SET ShortageAmount = ShortageCharged WHERE ShortageAmount = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortageAmount",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RouteShortagePayrollDeductionEnabled",
                table: "Companies");
        }
    }
}
