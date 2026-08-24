using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesAndBackfillReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsHeadOffice = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Seed the same 4 branches AppDbSeeder.SeedBranchesAsync seeds on a fresh database —
            // inserted here, before the FK constraints below, because Warehouses/DocumentSequences
            // (and possibly other tables) already have rows referencing these exact BranchId
            // strings ("br-hq", "br-north", ...); the AddForeignKey calls below would fail against
            // that pre-existing data if these rows didn't exist yet.
            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "Name", "Code", "City", "Address", "Phone", "Status", "IsHeadOffice" },
                values: new object[,]
                {
                    { "br-hq", "Head Office", "HQ", "Cebu City", "Osmena Blvd, Cebu City", "+63 32 111 2222", "active", true },
                    { "br-north", "North Branch", "NB", "Mandaue City", "A.S. Fortuna St, Mandaue City", "+63 32 222 3333", "active", false },
                    { "br-south", "South Branch", "SB", "Talisay City", "Tabunok, Talisay City", "+63 32 333 4444", "active", false },
                    { "br-east", "East Branch", "EB", "Lapu-Lapu City", "Pusok, Lapu-Lapu City", "+63 32 444 5555", "active", false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_DestBranchId",
                table: "StockTransferRequests",
                column: "DestBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferRequests_SourceBranchId",
                table: "StockTransferRequests",
                column: "SourceBranchId");

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
                name: "IX_GoodsReceipts_BranchId",
                table: "GoodsReceipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_BranchId",
                table: "GoodsIssues",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_DestBranchId",
                table: "GoodsIssues",
                column: "DestBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId",
                table: "Customers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Branches_BranchId",
                table: "Customers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentSequences_Branches_BranchId",
                table: "DocumentSequences",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssues_Branches_BranchId",
                table: "GoodsIssues",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssues_Branches_DestBranchId",
                table: "GoodsIssues",
                column: "DestBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipts_Branches_BranchId",
                table: "GoodsReceipts",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_Branches_BranchId",
                table: "StockAdjustments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLocationTransfers_Branches_BranchId",
                table: "StockLocationTransfers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockOpnames_Branches_BranchId",
                table: "StockOpnames",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferRequests_Branches_DestBranchId",
                table: "StockTransferRequests",
                column: "DestBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferRequests_Branches_SourceBranchId",
                table: "StockTransferRequests",
                column: "SourceBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Branches_BranchId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentSequences_Branches_BranchId",
                table: "DocumentSequences");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssues_Branches_BranchId",
                table: "GoodsIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssues_Branches_DestBranchId",
                table: "GoodsIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_Branches_BranchId",
                table: "GoodsReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_Branches_BranchId",
                table: "StockAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLocationTransfers_Branches_BranchId",
                table: "StockLocationTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockOpnames_Branches_BranchId",
                table: "StockOpnames");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferRequests_Branches_DestBranchId",
                table: "StockTransferRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferRequests_Branches_SourceBranchId",
                table: "StockTransferRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferRequests_DestBranchId",
                table: "StockTransferRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockTransferRequests_SourceBranchId",
                table: "StockTransferRequests");

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
                name: "IX_GoodsReceipts_BranchId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_BranchId",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_DestBranchId",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_Customers_BranchId",
                table: "Customers");
        }
    }
}
