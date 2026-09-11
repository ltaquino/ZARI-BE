using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanWriteOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanWriteOffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WriteOffNo = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoanAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WriteOffDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(14,4)", nullable: false),
                    WriteOffExpenseAccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_LoanWriteOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanWriteOffs_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanWriteOffs_GlAccounts_WriteOffExpenseAccountId",
                        column: x => x.WriteOffExpenseAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanWriteOffs_LoanAccounts_LoanAccountId",
                        column: x => x.LoanAccountId,
                        principalTable: "LoanAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoanWriteOffs_BranchId_WriteOffDate",
                table: "LoanWriteOffs",
                columns: new[] { "BranchId", "WriteOffDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanWriteOffs_LoanAccountId",
                table: "LoanWriteOffs",
                column: "LoanAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanWriteOffs_Status",
                table: "LoanWriteOffs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LoanWriteOffs_WriteOffExpenseAccountId",
                table: "LoanWriteOffs",
                column: "WriteOffExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanWriteOffs_WriteOffNo",
                table: "LoanWriteOffs",
                column: "WriteOffNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanWriteOffs");
        }
    }
}
