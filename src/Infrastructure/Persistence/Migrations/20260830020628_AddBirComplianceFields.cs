using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBirComplianceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegisteredAddress",
                table: "Companies",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                table: "Companies",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VatRegistrationType",
                table: "Companies",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BirBranchCode",
                table: "Branches",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MachineIdentificationNumber",
                table: "Branches",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MachineSerialNumber",
                table: "Branches",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PosPermitDateIssued",
                table: "Branches",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosPermitNumber",
                table: "Branches",
                type: "varchar(25)",
                maxLength: 25,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegisteredAddress",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TradeName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VatRegistrationType",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BirBranchCode",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "MachineIdentificationNumber",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "MachineSerialNumber",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "PosPermitDateIssued",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "PosPermitNumber",
                table: "Branches");
        }
    }
}
