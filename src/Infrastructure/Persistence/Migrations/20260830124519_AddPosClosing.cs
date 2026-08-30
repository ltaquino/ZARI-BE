using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZCounterValue = table.Column<int>(type: "int", nullable: false),
                    FirstOrNumber = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastOrNumber = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    InvoiceCount = table.Column<int>(type: "int", nullable: false),
                    GrossSales = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    TotalDiscounts = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    VatableSales = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    VatExemptSales = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    ZeroRatedSales = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    NetSales = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZReadings_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ZReadings_BranchId_ZCounterValue",
                table: "ZReadings",
                columns: new[] { "BranchId", "ZCounterValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZReadings");
        }
    }
}
