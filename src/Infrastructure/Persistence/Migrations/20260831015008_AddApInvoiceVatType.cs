using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApInvoiceVatType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VatType",
                table: "ApInvoiceLines",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "VATABLE")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VatType",
                table: "ApInvoiceExpenseLines",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "VATABLE")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatType",
                table: "ApInvoiceLines");

            migrationBuilder.DropColumn(
                name: "VatType",
                table: "ApInvoiceExpenseLines");
        }
    }
}
