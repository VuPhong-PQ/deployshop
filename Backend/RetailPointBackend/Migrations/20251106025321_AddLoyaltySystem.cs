using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPoints",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TierId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryLoyaltyRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    PointsMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryLoyaltyRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_CategoryLoyaltyRules_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinSpent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinPoints = table.Column<int>(type: "int", nullable: false),
                    PointsMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TierColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTiers", x => x.TierId);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyConfigs",
                columns: table => new
                {
                    LoyaltyConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PointsPerCurrency = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MinOrderAmountForPoints = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPointsPerOrder = table.Column<int>(type: "int", nullable: true),
                    PointExpiryDays = table.Column<int>(type: "int", nullable: false),
                    AllowPointRedemption = table.Column<bool>(type: "bit", nullable: false),
                    PointValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxRedemptionPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HappyHourEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HappyHourStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    HappyHourEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    HappyHourMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    WeekendBonusEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WeekendMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    BirthdayBonusEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BirthdayMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    BirthdayValidDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyConfigs", x => x.LoyaltyConfigId);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PointsBalance = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Staffs_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Staffs",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "ProductLoyaltyRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    PointsMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLoyaltyRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_ProductLoyaltyRules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPromotions",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PromotionType = table.Column<int>(type: "int", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: false),
                    TargetCustomerTier = table.Column<int>(type: "int", nullable: true),
                    BonusPoints = table.Column<int>(type: "int", nullable: false),
                    PointsMultiplier = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxUsagePerCustomer = table.Column<int>(type: "int", nullable: true),
                    MaxTotalUsage = table.Column<int>(type: "int", nullable: true),
                    CurrentUsage = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPromotions", x => x.PromotionId);
                    table.ForeignKey(
                        name: "FK_LoyaltyPromotions_CustomerTiers_TargetCustomerTier",
                        column: x => x.TargetCustomerTier,
                        principalTable: "CustomerTiers",
                        principalColumn: "TierId");
                    table.ForeignKey(
                        name: "FK_LoyaltyPromotions_Staffs_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Staffs",
                        principalColumn: "StaffId");
                });

            migrationBuilder.InsertData(
                table: "CustomerTiers",
                columns: new[] { "TierId", "CreatedAt", "Description", "DiscountPercentage", "IsActive", "MinPoints", "MinSpent", "PointsMultiplier", "TierColor", "TierName" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khách hàng mới", 0m, true, 0, 0m, 1.0m, "#CD7F32", "Đồng" },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khách hàng thân thiết", 2m, true, 500, 5000000m, 1.2m, "#C0C0C0", "Bạc" },
                    { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khách hàng VIP", 5m, true, 2000, 20000000m, 1.5m, "#FFD700", "Vàng" },
                    { 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khách hàng VVIP", 10m, true, 5000, 50000000m, 2.0m, "#B9F2FF", "Kim cương" }
                });

            migrationBuilder.InsertData(
                table: "LoyaltyConfigs",
                columns: new[] { "LoyaltyConfigId", "AllowPointRedemption", "BirthdayBonusEnabled", "BirthdayMultiplier", "BirthdayValidDays", "CreatedAt", "CreatedBy", "HappyHourEnabled", "HappyHourEndTime", "HappyHourMultiplier", "HappyHourStartTime", "IsEnabled", "MaxPointsPerOrder", "MaxRedemptionPercentage", "MinOrderAmountForPoints", "PointExpiryDays", "PointValue", "PointsPerCurrency", "UpdatedAt", "WeekendBonusEnabled", "WeekendMultiplier" },
                values: new object[] { 1, true, false, 3.0m, 7, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, new TimeSpan(0, 19, 0, 0, 0), 2.0m, new TimeSpan(0, 17, 0, 0, 0), true, null, 50.0m, 50000m, 365, 1000.0m, 1000.0m, null, false, 1.5m });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TierId",
                table: "Customers",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryLoyaltyRules_CategoryId",
                table: "CategoryLoyaltyRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPromotions_CreatedBy",
                table: "LoyaltyPromotions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPromotions_TargetCustomerTier",
                table: "LoyaltyPromotions",
                column: "TargetCustomerTier");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerId",
                table: "LoyaltyTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_OrderId",
                table: "LoyaltyTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_ProcessedBy",
                table: "LoyaltyTransactions",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLoyaltyRules_ProductId",
                table: "ProductLoyaltyRules",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_CustomerTiers_TierId",
                table: "Customers",
                column: "TierId",
                principalTable: "CustomerTiers",
                principalColumn: "TierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_CustomerTiers_TierId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "CategoryLoyaltyRules");

            migrationBuilder.DropTable(
                name: "LoyaltyConfigs");

            migrationBuilder.DropTable(
                name: "LoyaltyPromotions");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "ProductLoyaltyRules");

            migrationBuilder.DropTable(
                name: "CustomerTiers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TierId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LoyaltyPoints",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "Customers");
        }
    }
}
