using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCostCenterToTransactionalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "StockOpnames",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "StockAdjustments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "OutgoingPayments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "GoodsReturns",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "GoodsReceipts",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "GoodsReceiptPos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "GoodsIssues",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "ApInvoices",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_CostCenterId",
                table: "StockOpnames",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_CostCenterId",
                table: "StockAdjustments",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_OutgoingPayments_CostCenterId",
                table: "OutgoingPayments",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturns_CostCenterId",
                table: "GoodsReturns",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_CostCenterId",
                table: "GoodsReceipts",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptPos_CostCenterId",
                table: "GoodsReceiptPos",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_CostCenterId",
                table: "GoodsIssues",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoices_CostCenterId",
                table: "ApInvoices",
                column: "CostCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApInvoices_CostCenters_CostCenterId",
                table: "ApInvoices",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssues_CostCenters_CostCenterId",
                table: "GoodsIssues",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptPos_CostCenters_CostCenterId",
                table: "GoodsReceiptPos",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipts_CostCenters_CostCenterId",
                table: "GoodsReceipts",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReturns_CostCenters_CostCenterId",
                table: "GoodsReturns",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OutgoingPayments_CostCenters_CostCenterId",
                table: "OutgoingPayments",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_CostCenters_CostCenterId",
                table: "StockAdjustments",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockOpnames_CostCenters_CostCenterId",
                table: "StockOpnames",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApInvoices_CostCenters_CostCenterId",
                table: "ApInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssues_CostCenters_CostCenterId",
                table: "GoodsIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptPos_CostCenters_CostCenterId",
                table: "GoodsReceiptPos");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_CostCenters_CostCenterId",
                table: "GoodsReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReturns_CostCenters_CostCenterId",
                table: "GoodsReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_OutgoingPayments_CostCenters_CostCenterId",
                table: "OutgoingPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_CostCenters_CostCenterId",
                table: "StockAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockOpnames_CostCenters_CostCenterId",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_StockOpnames_CostCenterId",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_CostCenterId",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_OutgoingPayments_CostCenterId",
                table: "OutgoingPayments");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReturns_CostCenterId",
                table: "GoodsReturns");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_CostCenterId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptPos_CostCenterId",
                table: "GoodsReceiptPos");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_CostCenterId",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_ApInvoices_CostCenterId",
                table: "ApInvoices");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "StockOpnames");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "OutgoingPayments");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "GoodsReturns");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "GoodsReceiptPos");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "GoodsIssues");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "ApInvoices");
        }
    }
}
