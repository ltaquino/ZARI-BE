using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHrPayrollComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeAllowances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAllowances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDeductions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GovernmentContributionRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContributionType = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalaryFrom = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    SalaryTo = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EmployeeSharePct = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EmployerSharePct = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    MaxContribution = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: true),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_GovernmentContributionRates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SssContributionBrackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SalaryFrom = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    SalaryTo = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EmployeeShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EmployerShare = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_SssContributionBrackets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WithholdingTaxBrackets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SalaryFrom = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    SalaryTo = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    BaseTax = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    RateOverExcess = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_WithholdingTaxBrackets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAllowances_EmployeeId",
                table: "EmployeeAllowances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductions_EmployeeId",
                table: "EmployeeDeductions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentContributionRates_ContributionType_EffectiveDate_S~",
                table: "GovernmentContributionRates",
                columns: new[] { "ContributionType", "EffectiveDate", "SalaryFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_SssContributionBrackets_EffectiveDate_SalaryFrom",
                table: "SssContributionBrackets",
                columns: new[] { "EffectiveDate", "SalaryFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_WithholdingTaxBrackets_EffectiveDate_SalaryFrom",
                table: "WithholdingTaxBrackets",
                columns: new[] { "EffectiveDate", "SalaryFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeAllowances");

            migrationBuilder.DropTable(
                name: "EmployeeDeductions");

            migrationBuilder.DropTable(
                name: "GovernmentContributionRates");

            migrationBuilder.DropTable(
                name: "SssContributionBrackets");

            migrationBuilder.DropTable(
                name: "WithholdingTaxBrackets");
        }
    }
}
