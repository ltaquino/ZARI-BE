using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteSalesFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CounterScope",
                table: "ZReadings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "BRANCH")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "PosTerminalId",
                table: "ZReadings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "HasOwnSeries",
                table: "PosTerminals",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrNextNumber",
                table: "PosTerminals",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrPaddingLength",
                table: "PosTerminals",
                type: "int",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<string>(
                name: "OrPrefix",
                table: "PosTerminals",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ZCounter",
                table: "PosTerminals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Customers",
                type: "DECIMAL(14,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlateNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WarehouseId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PosTerminalId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
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
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_PosTerminals_PosTerminalId",
                        column: x => x.PosTerminalId,
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeliveryRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultVehicleId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
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
                    table.PrimaryKey("PK_DeliveryRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRoutes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryRoutes_Vehicles_DefaultVehicleId",
                        column: x => x.DefaultVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeliveryRouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RouteId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRouteStops_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryRouteStops_DeliveryRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "DeliveryRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ZReadings_BranchId_CounterScope_ZCounterValue",
                table: "ZReadings",
                columns: new[] { "BranchId", "CounterScope", "ZCounterValue" },
                unique: true);

            // Dropped only after the replacement index exists: MySQL refuses to drop an index the
            // FK_ZReadings_Branches_BranchId foreign key depends on unless another index can serve it.
            migrationBuilder.DropIndex(
                name: "IX_ZReadings_BranchId_ZCounterValue",
                table: "ZReadings");

            migrationBuilder.CreateIndex(
                name: "IX_ZReadings_PosTerminalId",
                table: "ZReadings",
                column: "PosTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_BranchId_Code",
                table: "DeliveryRoutes",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_DefaultVehicleId",
                table: "DeliveryRoutes",
                column: "DefaultVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRouteStops_CustomerId",
                table: "DeliveryRouteStops",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRouteStops_RouteId_CustomerId",
                table: "DeliveryRouteStops",
                columns: new[] { "RouteId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRouteStops_RouteId_Sequence",
                table: "DeliveryRouteStops",
                columns: new[] { "RouteId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_BranchId_Code",
                table: "Vehicles",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PlateNo",
                table: "Vehicles",
                column: "PlateNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PosTerminalId",
                table: "Vehicles",
                column: "PosTerminalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_WarehouseId",
                table: "Vehicles",
                column: "WarehouseId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ZReadings_PosTerminals_PosTerminalId",
                table: "ZReadings",
                column: "PosTerminalId",
                principalTable: "PosTerminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZReadings_PosTerminals_PosTerminalId",
                table: "ZReadings");

            migrationBuilder.DropTable(
                name: "DeliveryRouteStops");

            migrationBuilder.DropTable(
                name: "DeliveryRoutes");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.CreateIndex(
                name: "IX_ZReadings_BranchId_ZCounterValue",
                table: "ZReadings",
                columns: new[] { "BranchId", "ZCounterValue" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_ZReadings_BranchId_CounterScope_ZCounterValue",
                table: "ZReadings");

            migrationBuilder.DropIndex(
                name: "IX_ZReadings_PosTerminalId",
                table: "ZReadings");

            migrationBuilder.DropColumn(
                name: "CounterScope",
                table: "ZReadings");

            migrationBuilder.DropColumn(
                name: "PosTerminalId",
                table: "ZReadings");

            migrationBuilder.DropColumn(
                name: "HasOwnSeries",
                table: "PosTerminals");

            migrationBuilder.DropColumn(
                name: "OrNextNumber",
                table: "PosTerminals");

            migrationBuilder.DropColumn(
                name: "OrPaddingLength",
                table: "PosTerminals");

            migrationBuilder.DropColumn(
                name: "OrPrefix",
                table: "PosTerminals");

            migrationBuilder.DropColumn(
                name: "ZCounter",
                table: "PosTerminals");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Customers");
        }
    }
}
