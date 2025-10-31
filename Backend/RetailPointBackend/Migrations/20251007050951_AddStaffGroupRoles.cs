using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffGroupRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffGroupRoles",
                columns: table => new
                {
                    StaffGroupRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffGroupId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffGroupRoles", x => x.StaffGroupRoleId);
                    table.ForeignKey(
                        name: "FK_StaffGroupRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffGroupRoles_StaffGroups_StaffGroupId",
                        column: x => x.StaffGroupId,
                        principalTable: "StaffGroups",
                        principalColumn: "StaffGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffGroupRoles_RoleId",
                table: "StaffGroupRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffGroupRoles_StaffGroupId",
                table: "StaffGroupRoles",
                column: "StaffGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffGroupRoles");
        }
    }
}
