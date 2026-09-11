using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoanAcctNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoanProductId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoanApplicationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PrincipalAmount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    AnnualInterestRatePct = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    RepaymentFrequency = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    PenaltyRatePct = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    GrantDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    FirstDueDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoanReceivableAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    InterestIncomeAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PenaltyIncomeAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CancelledBy = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CancelReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
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
                    table.PrimaryKey("PK_LoanAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_GlAccounts_InterestIncomeAccountId",
                        column: x => x.InterestIncomeAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_GlAccounts_LoanReceivableAccountId",
                        column: x => x.LoanReceivableAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_GlAccounts_PenaltyIncomeAccountId",
                        column: x => x.PenaltyIncomeAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAccounts_LoanProducts_LoanProductId",
                        column: x => x.LoanProductId,
                        principalTable: "LoanProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoanAmortizationScheduleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoanAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InstallmentNo = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PrincipalDue = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    InterestDue = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    TotalDue = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    OutstandingPrincipalAfter = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    InterestPaid = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    Status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanAmortizationScheduleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanAmortizationScheduleLines_LoanAccounts_LoanAccountId",
                        column: x => x.LoanAccountId,
                        principalTable: "LoanAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_BranchId_GrantDate",
                table: "LoanAccounts",
                columns: new[] { "BranchId", "GrantDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_CustomerId",
                table: "LoanAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_InterestIncomeAccountId",
                table: "LoanAccounts",
                column: "InterestIncomeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_LoanAcctNo",
                table: "LoanAccounts",
                column: "LoanAcctNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_LoanApplicationId",
                table: "LoanAccounts",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_LoanProductId",
                table: "LoanAccounts",
                column: "LoanProductId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_LoanReceivableAccountId",
                table: "LoanAccounts",
                column: "LoanReceivableAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_PenaltyIncomeAccountId",
                table: "LoanAccounts",
                column: "PenaltyIncomeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAccounts_Status",
                table: "LoanAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAmortizationScheduleLines_DueDate",
                table: "LoanAmortizationScheduleLines",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAmortizationScheduleLines_LoanAccountId_InstallmentNo",
                table: "LoanAmortizationScheduleLines",
                columns: new[] { "LoanAccountId", "InstallmentNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanAmortizationScheduleLines");

            migrationBuilder.DropTable(
                name: "LoanAccounts");
        }
    }
}
