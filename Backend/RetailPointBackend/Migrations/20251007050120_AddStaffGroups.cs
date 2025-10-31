using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffGroups", x => x.StaffGroupId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_StaffGroupId",
                table: "Staffs",
                column: "StaffGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Staffs_StaffGroups_StaffGroupId",
                table: "Staffs",
                column: "StaffGroupId",
                principalTable: "StaffGroups",
                principalColumn: "StaffGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Staffs_StaffGroups_StaffGroupId",
                table: "Staffs");

            migrationBuilder.DropTable(
                name: "StaffGroups");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_StaffGroupId",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "StaffGroupId",
                table: "Staffs");
        }
    }
}
