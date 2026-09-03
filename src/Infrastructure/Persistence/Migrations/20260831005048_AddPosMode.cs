using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PosTerminalId",
                table: "SalesInvoices",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "MemberNo",
                table: "Customers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GlAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RequiresReferenceNo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReferenceNoLabel = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresBankOrPartnerName = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_GlAccounts_GlAccountId",
                        column: x => x.GlAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PosPromoSlides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subtitle = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PosPromoSlides", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PosTerminals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MachineIdentificationNumber = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MachineSerialNumber = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PosPermitNumber = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PosPermitDateIssued = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_PosTerminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosTerminals_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerPaymentTenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerPaymentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PaymentMethodId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    ReferenceNo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BankOrPartnerName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPaymentTenders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPaymentTenders_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPaymentTenders_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_PosTerminalId",
                table: "SalesInvoices",
                column: "PosTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentTenders_CustomerPaymentId",
                table: "CustomerPaymentTenders",
                column: "CustomerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPaymentTenders_PaymentMethodId",
                table: "CustomerPaymentTenders",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Code",
                table: "PaymentMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_GlAccountId",
                table: "PaymentMethods",
                column: "GlAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_BranchId_Code",
                table: "PosTerminals",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_PosTerminals_PosTerminalId",
                table: "SalesInvoices",
                column: "PosTerminalId",
                principalTable: "PosTerminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_PosTerminals_PosTerminalId",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "CustomerPaymentTenders");

            migrationBuilder.DropTable(
                name: "PosPromoSlides");

            migrationBuilder.DropTable(
                name: "PosTerminals");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_PosTerminalId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "PosTerminalId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "MemberNo",
                table: "Customers");
        }
    }
}
