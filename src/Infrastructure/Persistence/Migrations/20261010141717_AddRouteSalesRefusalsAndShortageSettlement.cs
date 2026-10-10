using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteSalesRefusalsAndShortageSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShortagePaid",
                table: "Trips",
                type: "DECIMAL(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShortageSettlement",
                table: "Trips",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "QtyRefused",
                table: "TripLines",
                type: "DECIMAL(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TripRefusals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TripId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClientRef = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SalesOrderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RouteSaleId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RefusedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRefusals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRefusals_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripRefusals_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripRefusals_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TripShortagePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TripId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReferenceNo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripShortagePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripShortagePayments_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripShortagePayments_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TripRefusalLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TripRefusalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SalesOrderLineId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Qty = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    ReasonCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRefusalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRefusalLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripRefusalLines_TripRefusals_TripRefusalId",
                        column: x => x.TripRefusalId,
                        principalTable: "TripRefusals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusalLines_ItemId",
                table: "TripRefusalLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusalLines_SalesOrderLineId",
                table: "TripRefusalLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusalLines_TripRefusalId",
                table: "TripRefusalLines",
                column: "TripRefusalId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusals_ClientRef",
                table: "TripRefusals",
                column: "ClientRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusals_CustomerId",
                table: "TripRefusals",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusals_SalesOrderId",
                table: "TripRefusals",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRefusals_TripId_SalesOrderId",
                table: "TripRefusals",
                columns: new[] { "TripId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TripShortagePayments_PaymentMethodId",
                table: "TripShortagePayments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_TripShortagePayments_TripId",
                table: "TripShortagePayments",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripRefusalLines");

            migrationBuilder.DropTable(
                name: "TripShortagePayments");

            migrationBuilder.DropTable(
                name: "TripRefusals");

            migrationBuilder.DropColumn(
                name: "ShortagePaid",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ShortageSettlement",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "QtyRefused",
                table: "TripLines");
        }
    }
}
