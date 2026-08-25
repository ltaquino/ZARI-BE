using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZARI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersRolesPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AspNetUsers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AspNetUsers",
                type: "varchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Status just got a NOT NULL constraint with defaultValue "" for the migration itself —
            // every account seeded by AppDbSeeder before this migration existed is actually active,
            // so backfill it here rather than leaving pre-existing demo accounts with a blank status.
            migrationBuilder.Sql("UPDATE AspNetUsers SET Status = 'active' WHERE Status = '';");

            migrationBuilder.CreateTable(
                name: "Forms",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Module = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.Code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserBranches",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranches", x => new { x.UserId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_UserBranches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBranches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Backfill branch assignments + phone numbers for demo accounts SeedDemoUsersAsync
            // already created in earlier migrations — that seeder only touches users it creates,
            // so pre-existing demo rows would otherwise be left with zero branches after this
            // migration adds the concept. JOINs are no-ops (0 rows affected) on a fresh database
            // where none of these emails exist yet — SeedDemoUsersAsync handles that case itself.
            migrationBuilder.Sql(
                """
                INSERT INTO UserBranches (UserId, BranchId)
                SELECT u.Id, m.BranchId
                FROM AspNetUsers u
                JOIN (
                    SELECT 'admin@zari.coop' AS Email, 'br-hq' AS BranchId
                    UNION ALL SELECT 'admin@zari.coop', 'br-north'
                    UNION ALL SELECT 'admin@zari.coop', 'br-south'
                    UNION ALL SELECT 'admin@zari.coop', 'br-east'
                    UNION ALL SELECT 'manager@zari.coop', 'br-north'
                    UNION ALL SELECT 'ana.lopez@zari.coop', 'br-south'
                    UNION ALL SELECT 'rico.tan@zari.coop', 'br-east'
                    UNION ALL SELECT 'staff.north@zari.coop', 'br-north'
                    UNION ALL SELECT 'manager.hq@zari.coop', 'br-hq'
                    UNION ALL SELECT 'staff.hq@zari.coop', 'br-hq'
                ) m ON m.Email = u.Email;
                """);

            migrationBuilder.Sql(
                """
                UPDATE AspNetUsers u
                JOIN (
                    SELECT 'admin@zari.coop' AS Email, '+63 917 111 2222' AS Phone
                    UNION ALL SELECT 'manager@zari.coop', '+63 918 222 3333'
                    UNION ALL SELECT 'ana.lopez@zari.coop', '+63 919 333 4444'
                    UNION ALL SELECT 'rico.tan@zari.coop', '+63 920 444 5555'
                    UNION ALL SELECT 'staff.north@zari.coop', '+63 921 555 6666'
                    UNION ALL SELECT 'manager.hq@zari.coop', '+63 922 666 7777'
                    UNION ALL SELECT 'staff.hq@zari.coop', '+63 923 777 8888'
                ) m ON m.Email = u.Email
                SET u.Phone = m.Phone
                WHERE u.Phone IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormCode = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanView = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanCreate = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanEdit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanApprove = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanCancel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.FormCode });
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Forms_FormCode",
                        column: x => x.FormCode,
                        principalTable: "Forms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserFormPermissionOverrides",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormCode = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanView = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanCreate = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanEdit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanApprove = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanCancel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFormPermissionOverrides", x => new { x.UserId, x.FormCode });
                    table.ForeignKey(
                        name: "FK_UserFormPermissionOverrides_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFormPermissionOverrides_Forms_FormCode",
                        column: x => x.FormCode,
                        principalTable: "Forms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_FormCode",
                table: "RolePermissions",
                column: "FormCode");

            migrationBuilder.CreateIndex(
                name: "IX_UserBranches_BranchId",
                table: "UserBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFormPermissionOverrides_FormCode",
                table: "UserFormPermissionOverrides",
                column: "FormCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserBranches");

            migrationBuilder.DropTable(
                name: "UserFormPermissionOverrides");

            migrationBuilder.DropTable(
                name: "Forms");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AspNetUsers");
        }
    }
}
