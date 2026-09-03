using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesSerialNoTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerialNo",
                table: "SalesReturnLines",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SerialNo",
                table: "SalesInvoiceLines",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerialNo",
                table: "SalesReturnLines");

            migrationBuilder.DropColumn(
                name: "SerialNo",
                table: "SalesInvoiceLines");
        }
    }
}
