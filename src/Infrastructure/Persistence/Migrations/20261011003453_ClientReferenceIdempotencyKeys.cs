using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClientReferenceIdempotencyKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientReferenceId",
                table: "StockOpnames",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientReferenceId",
                table: "StockAdjustments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientReferenceId",
                table: "SalesOrders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientReferenceId",
                table: "SalesInvoices",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_ClientReferenceId",
                table: "StockOpnames",
                column: "ClientReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ClientReferenceId",
                table: "StockAdjustments",
                column: "ClientReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ClientReferenceId",
                table: "SalesOrders",
                column: "ClientReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_ClientReferenceId",
                table: "SalesInvoices",
                column: "ClientReferenceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockOpnames_ClientReferenceId",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_ClientReferenceId",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_ClientReferenceId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_ClientReferenceId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ClientReferenceId",
                table: "StockOpnames");

            migrationBuilder.DropColumn(
                name: "ClientReferenceId",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "ClientReferenceId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ClientReferenceId",
                table: "SalesInvoices");
        }
    }
}
