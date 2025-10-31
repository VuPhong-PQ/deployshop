using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationReasonToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_StaffGroups_StaffGroupId",
                table: "Staffs");

            migrationBuilder.DropTable(
                name: "StaffGroupRoles");

            migrationBuilder.DropTable(
                name: "StaffGroups");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_StaffGroupId",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "StaffGroupId",
                table: "Staffs");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "StaffGroupId",
                table: "Staffs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StaffGroups",
                columns: table => new
                {
                    StaffGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffGroups", x => x.StaffGroupId);
                });

            migrationBuilder.CreateTable(
                name: "StaffGroupRoles",
                columns: table => new
                {
                    StaffGroupRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    StaffGroupId = table.Column<int>(type: "int", nullable: false),
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
                name: "IX_Staffs_StaffGroupId",
                table: "Staffs",
                column: "StaffGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffGroupRoles_RoleId",
                table: "StaffGroupRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffGroupRoles_StaffGroupId",
                table: "StaffGroupRoles",
                column: "StaffGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_StaffGroups_StaffGroupId",
                table: "Staffs",
                column: "StaffGroupId",
                principalTable: "StaffGroups",
                principalColumn: "StaffGroupId");
        }
    }
}
