using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollLineAbsenceHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AbsenceDeduction",
                table: "PayrollLines",
                type: "DECIMAL(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LeaveDaysConsumed",
                table: "PayrollLines",
                type: "DECIMAL(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaveTypeConsumedId",
                table: "PayrollLines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLines_LeaveTypeConsumedId",
                table: "PayrollLines",
                column: "LeaveTypeConsumedId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollLines_LeaveTypes_LeaveTypeConsumedId",
                table: "PayrollLines",
                column: "LeaveTypeConsumedId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollLines_LeaveTypes_LeaveTypeConsumedId",
                table: "PayrollLines");

            migrationBuilder.DropIndex(
                name: "IX_PayrollLines_LeaveTypeConsumedId",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "AbsenceDeduction",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "LeaveDaysConsumed",
                table: "PayrollLines");

            migrationBuilder.DropColumn(
                name: "LeaveTypeConsumedId",
                table: "PayrollLines");
        }
    }
}
