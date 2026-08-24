using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBranchReferencesRemaining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId",
                table: "StockReservations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgers_BranchId",
                table: "StockLedgers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_BranchId",
                table: "StockBalances",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BranchId",
                table: "Notifications",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBranchSettings_BranchId",
                table: "ItemBranchSettings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GlJournals_BranchId",
                table: "GlJournals",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BranchId",
                table: "CostCenters",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_BranchId",
                table: "ApprovalRequests",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Branches_BranchId",
                table: "ApprovalRequests",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CostCenters_Branches_BranchId",
                table: "CostCenters",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GlJournals_Branches_BranchId",
                table: "GlJournals",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemBranchSettings_Branches_BranchId",
                table: "ItemBranchSettings",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Branches_BranchId",
                table: "Notifications",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockBalances_Branches_BranchId",
                table: "StockBalances",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgers_Branches_BranchId",
                table: "StockLedgers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockReservations_Branches_BranchId",
                table: "StockReservations",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Branches_BranchId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CostCenters_Branches_BranchId",
                table: "CostCenters");

            migrationBuilder.DropForeignKey(
                name: "FK_GlJournals_Branches_BranchId",
                table: "GlJournals");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemBranchSettings_Branches_BranchId",
                table: "ItemBranchSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Branches_BranchId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_StockBalances_Branches_BranchId",
                table: "StockBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgers_Branches_BranchId",
                table: "StockLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReservations_Branches_BranchId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockReservations_BranchId",
                table: "StockReservations");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgers_BranchId",
                table: "StockLedgers");

            migrationBuilder.DropIndex(
                name: "IX_StockBalances_BranchId",
                table: "StockBalances");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BranchId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ItemBranchSettings_BranchId",
                table: "ItemBranchSettings");

            migrationBuilder.DropIndex(
                name: "IX_GlJournals_BranchId",
                table: "GlJournals");

            migrationBuilder.DropIndex(
                name: "IX_CostCenters_BranchId",
                table: "CostCenters");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_BranchId",
                table: "ApprovalRequests");
        }
    }
}
