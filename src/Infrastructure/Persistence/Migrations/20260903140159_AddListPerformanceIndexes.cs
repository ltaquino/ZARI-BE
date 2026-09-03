using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_SourceBranchId_RequestDate",
                table: "StockTransferRequests",
                columns: new[] { "SourceBranchId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_Status",
                table: "StockTransferRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId_ReservedDate",
                table: "StockReservations",
                columns: new[] { "BranchId", "ReservedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_Status",
                table: "StockReservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_BranchId_CountDate",
                table: "StockOpnames",
                columns: new[] { "BranchId", "CountDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_Status",
                table: "StockOpnames",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocationTransfers_BranchId_TransferDate",
                table: "StockLocationTransfers",
                columns: new[] { "BranchId", "TransferDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLocationTransfers_Status",
                table: "StockLocationTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_BranchId_AdjustmentDate",
                table: "StockAdjustments",
                columns: new[] { "BranchId", "AdjustmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_Status",
                table: "StockAdjustments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_BranchId_ReturnDate",
                table: "SalesReturns",
                columns: new[] { "BranchId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_Status",
                table: "SalesReturns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_BranchId_OrderDate",
                table: "SalesOrders",
                columns: new[] { "BranchId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Status",
                table: "SalesOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId_InvoiceDate",
                table: "SalesInvoices",
                columns: new[] { "BranchId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_Status",
                table: "SalesInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_BranchId_RequestDate",
                table: "PurchaseRequests",
                columns: new[] { "BranchId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_Status",
                table: "PurchaseRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId_OrderDate",
                table: "PurchaseOrders",
                columns: new[] { "BranchId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutgoingPayments_BranchId_PaymentDate",
                table: "OutgoingPayments",
                columns: new[] { "BranchId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OutgoingPayments_Status",
                table: "OutgoingPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ManualJournalEntries_BranchId_EntryDate",
                table: "ManualJournalEntries",
                columns: new[] { "BranchId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualJournalEntries_Status",
                table: "ManualJournalEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status",
                table: "Items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturns_BranchId_ReturnDate",
                table: "GoodsReturns",
                columns: new[] { "BranchId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturns_Status",
                table: "GoodsReturns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_BranchId_GrDate",
                table: "GoodsReceipts",
                columns: new[] { "BranchId", "GrDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_Status",
                table: "GoodsReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptPos_BranchId_ReceiptDate",
                table: "GoodsReceiptPos",
                columns: new[] { "BranchId", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptPos_Status",
                table: "GoodsReceiptPos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_BranchId_GiDate",
                table: "GoodsIssues",
                columns: new[] { "BranchId", "GiDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_Status",
                table: "GoodsIssues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GlJournals_BranchId_JournalDate",
                table: "GlJournals",
                columns: new[] { "BranchId", "JournalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GlJournals_Status",
                table: "GlJournals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_BranchId_DeliveryDate",
                table: "DeliveryOrders",
                columns: new[] { "BranchId", "DeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_Status",
                table: "DeliveryOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_BranchId_PaymentDate",
                table: "CustomerPayments",
                columns: new[] { "BranchId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_Status",
                table: "CustomerPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoices_BranchId_InvoiceDate",
                table: "ApInvoices",
                columns: new[] { "BranchId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoices_Status",
                table: "ApInvoices",
                column: "Status");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferRequests_SourceBranchId",
                table: "StockTransferRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_BranchId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockOpnames_BranchId",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_StockLocationTransfers_BranchId",
                table: "StockLocationTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_BranchId",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_BranchId",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_BranchId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_BranchId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_BranchId",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_BranchId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_OutgoingPayments_BranchId",
                table: "OutgoingPayments");

            migrationBuilder.DropIndex(
                name: "IX_ManualJournalEntries_BranchId",
                table: "ManualJournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReturns_BranchId",
                table: "GoodsReturns");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_BranchId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptPos_BranchId",
                table: "GoodsReceiptPos");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_BranchId",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GlJournals_BranchId",
                table: "GlJournals");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryOrders_BranchId",
                table: "DeliveryOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_BranchId",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_ApInvoices_BranchId",
                table: "ApInvoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_SourceBranchId",
                table: "StockTransferRequests",
                column: "SourceBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId",
                table: "StockReservations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_BranchId",
                table: "StockOpnames",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocationTransfers_BranchId",
                table: "StockLocationTransfers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_BranchId",
                table: "StockAdjustments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_BranchId",
                table: "SalesReturns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_BranchId",
                table: "SalesOrders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId",
                table: "SalesInvoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_BranchId",
                table: "PurchaseRequests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId",
                table: "PurchaseOrders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OutgoingPayments_BranchId",
                table: "OutgoingPayments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualJournalEntries_BranchId",
                table: "ManualJournalEntries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturns_BranchId",
                table: "GoodsReturns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_BranchId",
                table: "GoodsReceipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptPos_BranchId",
                table: "GoodsReceiptPos",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_BranchId",
                table: "GoodsIssues",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GlJournals_BranchId",
                table: "GlJournals",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_BranchId",
                table: "DeliveryOrders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_BranchId",
                table: "CustomerPayments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoices_BranchId",
                table: "ApInvoices",
                column: "BranchId");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferRequests_SourceBranchId_RequestDate",
                table: "StockTransferRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferRequests_Status",
                table: "StockTransferRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_BranchId_ReservedDate",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_Status",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockOpnames_BranchId_CountDate",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_StockOpnames_Status",
                table: "StockOpnames");

            migrationBuilder.DropIndex(
                name: "IX_StockLocationTransfers_BranchId_TransferDate",
                table: "StockLocationTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockLocationTransfers_Status",
                table: "StockLocationTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_BranchId_AdjustmentDate",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustments_Status",
                table: "StockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_BranchId_ReturnDate",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_Status",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_BranchId_OrderDate",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_Status",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_BranchId_InvoiceDate",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_Status",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_BranchId_RequestDate",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_Status",
                table: "PurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_BranchId_OrderDate",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_OutgoingPayments_BranchId_PaymentDate",
                table: "OutgoingPayments");

            migrationBuilder.DropIndex(
                name: "IX_OutgoingPayments_Status",
                table: "OutgoingPayments");

            migrationBuilder.DropIndex(
                name: "IX_ManualJournalEntries_BranchId_EntryDate",
                table: "ManualJournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_ManualJournalEntries_Status",
                table: "ManualJournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Items_Name",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Status",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReturns_BranchId_ReturnDate",
                table: "GoodsReturns");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReturns_Status",
                table: "GoodsReturns");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_BranchId_GrDate",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_Status",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptPos_BranchId_ReceiptDate",
                table: "GoodsReceiptPos");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptPos_Status",
                table: "GoodsReceiptPos");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_BranchId_GiDate",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_Status",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GlJournals_BranchId_JournalDate",
                table: "GlJournals");

            migrationBuilder.DropIndex(
                name: "IX_GlJournals_Status",
                table: "GlJournals");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryOrders_BranchId_DeliveryDate",
                table: "DeliveryOrders");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryOrders_Status",
                table: "DeliveryOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_BranchId_PaymentDate",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPayments_Status",
                table: "CustomerPayments");

            migrationBuilder.DropIndex(
                name: "IX_ApInvoices_BranchId_InvoiceDate",
                table: "ApInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ApInvoices_Status",
                table: "ApInvoices");
        }
    }
}
