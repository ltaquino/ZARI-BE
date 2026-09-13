using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHrPayrollRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PayrollRunNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayPeriodStart = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PayPeriodEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PayrollLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PayrollRunId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BasicPay = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    OvertimePay = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    AllowanceTotal = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    GrossPay = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    SssEmployeeShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    SssEmployerShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PhilHealthEmployeeShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PhilHealthEmployerShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PagIbigEmployeeShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PagIbigEmployerShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    WithholdingTax = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    DeductionTotal = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    NetPay = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollLines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollLines_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLines_EmployeeId",
                table: "PayrollLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLines_PayrollRunId_EmployeeId",
                table: "PayrollLines",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_BranchId",
                table: "PayrollRuns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayrollRunNo",
                table: "PayrollRuns",
                column: "PayrollRunNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Status",
                table: "PayrollRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollLines");

            migrationBuilder.DropTable(
                name: "PayrollRuns");
        }
    }
}
