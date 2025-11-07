using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoyaltySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsPointsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PointsRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsRedemptionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RedemptionRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxRedemptionPercentage = table.Column<int>(type: "int", nullable: false),
                    MaxPointsPerOrder = table.Column<int>(type: "int", nullable: false),
                    PointsExpirationDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltySettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "LoyaltySettings",
                columns: new[] { "Id", "CreatedAt", "IsPointsEnabled", "IsRedemptionEnabled", "MaxPointsPerOrder", "MaxRedemptionPercentage", "MinOrderAmount", "Notes", "PointsExpirationDays", "PointsRate", "RedemptionRate", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 0, 50, 50000m, "Cài đặt tích điểm mặc định", 365, 1000m, 1000m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltySettings");
        }
    }
}
