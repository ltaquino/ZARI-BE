using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountRuleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountRules_Items_ItemId",
                table: "DiscountRules");

            migrationBuilder.DropIndex(
                name: "IX_DiscountRules_ItemId",
                table: "DiscountRules");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "DiscountRules");

            migrationBuilder.CreateTable(
                name: "DiscountRuleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DiscountRuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRuleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountRuleItems_DiscountRules_DiscountRuleId",
                        column: x => x.DiscountRuleId,
                        principalTable: "DiscountRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountRuleItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRuleItems_DiscountRuleId_ItemId",
                table: "DiscountRuleItems",
                columns: new[] { "DiscountRuleId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRuleItems_ItemId",
                table: "DiscountRuleItems",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountRuleItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "DiscountRules",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_ItemId",
                table: "DiscountRules",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountRules_Items_ItemId",
                table: "DiscountRules",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
