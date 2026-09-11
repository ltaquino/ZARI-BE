using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyPaid",
                table: "LoanAmortizationScheduleLines",
                type: "DECIMAL(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "LoanPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PaymentNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoanAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PaymentDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReferenceNo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CostCenterId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_LoanPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPayments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayments_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayments_LoanAccounts_LoanAccountId",
                        column: x => x.LoanAccountId,
                        principalTable: "LoanAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPayments_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoanPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoanPaymentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoanAmortizationScheduleLineId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PrincipalApplied = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    InterestApplied = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    PenaltyApplied = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPaymentAllocations_LoanAmortizationScheduleLines_LoanAmo~",
                        column: x => x.LoanAmortizationScheduleLineId,
                        principalTable: "LoanAmortizationScheduleLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPaymentAllocations_LoanPayments_LoanPaymentId",
                        column: x => x.LoanPaymentId,
                        principalTable: "LoanPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentAllocations_LoanAmortizationScheduleLineId",
                table: "LoanPaymentAllocations",
                column: "LoanAmortizationScheduleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentAllocations_LoanPaymentId",
                table: "LoanPaymentAllocations",
                column: "LoanPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_BranchId_PaymentDate",
                table: "LoanPayments",
                columns: new[] { "BranchId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_CostCenterId",
                table: "LoanPayments",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_LoanAccountId",
                table: "LoanPayments",
                column: "LoanAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_PaymentMethodId",
                table: "LoanPayments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_PaymentNo",
                table: "LoanPayments",
                column: "PaymentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_Status",
                table: "LoanPayments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanPaymentAllocations");

            migrationBuilder.DropTable(
                name: "LoanPayments");

            migrationBuilder.DropColumn(
                name: "PenaltyPaid",
                table: "LoanAmortizationScheduleLines");
        }
    }
}
