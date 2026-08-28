using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApInvoiceExpenseLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptPoId",
                table: "ApInvoices",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            // Every pre-existing row was created before this feature existed and is an item invoice
            // (billed against a GRPO) — default them to "ITEM" rather than an empty string so they
            // stay valid against the app-level InvoiceType checks/filters going forward.
            migrationBuilder.AddColumn<string>(
                name: "InvoiceType",
                table: "ApInvoices",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "ITEM")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApInvoiceExpenseLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ApInvoiceId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GlAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApInvoiceExpenseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApInvoiceExpenseLines_ApInvoices_ApInvoiceId",
                        column: x => x.ApInvoiceId,
                        principalTable: "ApInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApInvoiceExpenseLines_GlAccounts_GlAccountId",
                        column: x => x.GlAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoiceExpenseLines_ApInvoiceId",
                table: "ApInvoiceExpenseLines",
                column: "ApInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApInvoiceExpenseLines_GlAccountId",
                table: "ApInvoiceExpenseLines",
                column: "GlAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApInvoiceExpenseLines");

            migrationBuilder.DropColumn(
                name: "InvoiceType",
                table: "ApInvoices");

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptPoId",
                table: "ApInvoices",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}
