using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanDisbursement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanDisbursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DisbursementNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoanAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DisbursementDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_LoanDisbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanDisbursements_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDisbursements_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDisbursements_LoanAccounts_LoanAccountId",
                        column: x => x.LoanAccountId,
                        principalTable: "LoanAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDisbursements_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_BranchId_DisbursementDate",
                table: "LoanDisbursements",
                columns: new[] { "BranchId", "DisbursementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_CostCenterId",
                table: "LoanDisbursements",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_DisbursementNo",
                table: "LoanDisbursements",
                column: "DisbursementNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_LoanAccountId",
                table: "LoanDisbursements",
                column: "LoanAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_PaymentMethodId",
                table: "LoanDisbursements",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_Status",
                table: "LoanDisbursements",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanDisbursements");
        }
    }
}
