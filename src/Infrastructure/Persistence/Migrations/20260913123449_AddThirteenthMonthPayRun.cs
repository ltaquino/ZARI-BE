using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThirteenthMonthPayRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThirteenthMonthPayRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RunNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PayDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_ThirteenthMonthPayRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThirteenthMonthPayRuns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThirteenthMonthPayLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ThirteenthMonthPayRunId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TotalBasicPay = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    ThirteenthMonthPay = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirteenthMonthPayLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThirteenthMonthPayLines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThirteenthMonthPayLines_ThirteenthMonthPayRuns_ThirteenthMon~",
                        column: x => x.ThirteenthMonthPayRunId,
                        principalTable: "ThirteenthMonthPayRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ThirteenthMonthPayLines_EmployeeId",
                table: "ThirteenthMonthPayLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ThirteenthMonthPayLines_ThirteenthMonthPayRunId_EmployeeId",
                table: "ThirteenthMonthPayLines",
                columns: new[] { "ThirteenthMonthPayRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThirteenthMonthPayRuns_BranchId_Year",
                table: "ThirteenthMonthPayRuns",
                columns: new[] { "BranchId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThirteenthMonthPayRuns_RunNo",
                table: "ThirteenthMonthPayRuns",
                column: "RunNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThirteenthMonthPayRuns_Status",
                table: "ThirteenthMonthPayRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThirteenthMonthPayLines");

            migrationBuilder.DropTable(
                name: "ThirteenthMonthPayRuns");
        }
    }
}
