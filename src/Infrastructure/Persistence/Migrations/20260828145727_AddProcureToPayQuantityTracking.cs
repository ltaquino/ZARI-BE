using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcureToPayQuantityTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseRequestLineId",
                table: "PurchaseOrderLines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptPoLineId",
                table: "GoodsReturnLines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderLineId",
                table: "GoodsReceiptPoLines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptPoLineId",
                table: "ApInvoiceLines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseRequestLineId",
                table: "PurchaseOrderLines",
                column: "PurchaseRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturnLines_GoodsReceiptPoLineId",
                table: "GoodsReturnLines",
                column: "GoodsReceiptPoLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptPoLines_PurchaseOrderLineId",
                table: "GoodsReceiptPoLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoiceLines_GoodsReceiptPoLineId",
                table: "ApInvoiceLines",
                column: "GoodsReceiptPoLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApInvoiceLines_GoodsReceiptPoLines_GoodsReceiptPoLineId",
                table: "ApInvoiceLines",
                column: "GoodsReceiptPoLineId",
                principalTable: "GoodsReceiptPoLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptPoLines_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptPoLines",
                column: "PurchaseOrderLineId",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReturnLines_GoodsReceiptPoLines_GoodsReceiptPoLineId",
                table: "GoodsReturnLines",
                column: "GoodsReceiptPoLineId",
                principalTable: "GoodsReceiptPoLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseRequestLines_PurchaseRequestLineId",
                table: "PurchaseOrderLines",
                column: "PurchaseRequestLineId",
                principalTable: "PurchaseRequestLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApInvoiceLines_GoodsReceiptPoLines_GoodsReceiptPoLineId",
                table: "ApInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptPoLines_PurchaseOrderLines_PurchaseOrderLineId",
                table: "GoodsReceiptPoLines");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReturnLines_GoodsReceiptPoLines_GoodsReceiptPoLineId",
                table: "GoodsReturnLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_PurchaseRequestLines_PurchaseRequestLineId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderLines_PurchaseRequestLineId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReturnLines_GoodsReceiptPoLineId",
                table: "GoodsReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptPoLines_PurchaseOrderLineId",
                table: "GoodsReceiptPoLines");

            migrationBuilder.DropIndex(
                name: "IX_ApInvoiceLines_GoodsReceiptPoLineId",
                table: "ApInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PurchaseRequestLineId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptPoLineId",
                table: "GoodsReturnLines");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderLineId",
                table: "GoodsReceiptPoLines");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptPoLineId",
                table: "ApInvoiceLines");
        }
    }
}
