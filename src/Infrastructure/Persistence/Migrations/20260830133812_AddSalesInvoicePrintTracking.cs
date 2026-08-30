using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoicePrintTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstPrintedAt",
                table: "SalesInvoices",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPrintedAt",
                table: "SalesInvoices",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintCount",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstPrintedAt",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "LastPrintedAt",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "PrintCount",
                table: "SalesInvoices");
        }
    }
}
