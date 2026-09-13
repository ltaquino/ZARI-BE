using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InterestMethod = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnnualInterestRatePct = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    MinPrincipal = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    MaxPrincipal = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    MinTermMonths = table.Column<int>(type: "int", nullable: false),
                    MaxTermMonths = table.Column<int>(type: "int", nullable: false),
                    RepaymentFrequency = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    PenaltyRatePct = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    RequiresCollateral = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequiresCoMaker = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LoanReceivableAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    InterestIncomeAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PenaltyIncomeAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_LoanProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanProducts_GlAccounts_InterestIncomeAccountId",
                        column: x => x.InterestIncomeAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanProducts_GlAccounts_LoanReceivableAccountId",
                        column: x => x.LoanReceivableAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanProducts_GlAccounts_PenaltyIncomeAccountId",
                        column: x => x.PenaltyIncomeAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_Code",
                table: "LoanProducts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_InterestIncomeAccountId",
                table: "LoanProducts",
                column: "InterestIncomeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_LoanReceivableAccountId",
                table: "LoanProducts",
                column: "LoanReceivableAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_PenaltyIncomeAccountId",
                table: "LoanProducts",
                column: "PenaltyIncomeAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanProducts");
        }
    }
}
