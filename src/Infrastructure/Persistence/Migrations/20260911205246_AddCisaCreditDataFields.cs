using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCisaCreditDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeNotes",
                table: "LoanAccounts",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsDisputed",
                table: "LoanAccounts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountInfo",
                table: "Customers",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CivilStatus",
                table: "Customers",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "DataSharingConsent",
                table: "Customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataSharingConsentDate",
                table: "Customers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateOfBirth",
                table: "Customers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependentsCount",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Employer",
                table: "Customers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmployerPosition",
                table: "Customers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmploymentSince",
                table: "Customers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HousingStatus",
                table: "Customers",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "NetIncomeLastYear",
                table: "Customers",
                type: "DECIMAL(14,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherAssetsNotes",
                table: "Customers",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "OwnsVehicle",
                table: "Customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PriorEmploymentHistory",
                table: "Customers",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PriorResidenceHistory",
                table: "Customers",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResidenceSince",
                table: "Customers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "Customers",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SssOrGsisNo",
                table: "Customers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Tin",
                table: "Customers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerCreditRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RecordType = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: true),
                    Remarks = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
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
                    table.PrimaryKey("PK_CustomerCreditRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCreditRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditRecords_CustomerId",
                table: "CustomerCreditRecords",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditRecords_RecordType",
                table: "CustomerCreditRecords",
                column: "RecordType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerCreditRecords");

            migrationBuilder.DropColumn(
                name: "DisputeNotes",
                table: "LoanAccounts");

            migrationBuilder.DropColumn(
                name: "IsDisputed",
                table: "LoanAccounts");

            migrationBuilder.DropColumn(
                name: "BankAccountInfo",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CivilStatus",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DataSharingConsent",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DataSharingConsentDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DependentsCount",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Employer",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmployerPosition",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmploymentSince",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HousingStatus",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NetIncomeLastYear",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OtherAssetsNotes",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OwnsVehicle",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PriorEmploymentHistory",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PriorResidenceHistory",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ResidenceSince",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SssOrGsisNo",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Tin",
                table: "Customers");
        }
    }
}
