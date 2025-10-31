using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddEInvoiceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EInvoiceConfigs",
                columns: table => new
                {
                    EInvoiceConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultTemplate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AutoIssue = table.Column<bool>(type: "bit", nullable: false),
                    AutoSendEmail = table.Column<bool>(type: "bit", nullable: false),
                    AutoSendSMS = table.Column<bool>(type: "bit", nullable: false),
                    CompanyTaxCode = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyBankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyBankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultTaxRate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EmailTemplate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SMSTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByStaffId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EInvoiceConfigs", x => x.EInvoiceConfigId);
                    table.ForeignKey(
                        name: "FK_EInvoiceConfigs_Staffs_CreatedByStaffId",
                        column: x => x.CreatedByStaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "EInvoices",
                columns: table => new
                {
                    EInvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceTemplate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvoiceSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellerTaxCode = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    SellerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SellerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SellerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SellerEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SellerBankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SellerBankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BuyerTaxCode = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    BuyerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BuyerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BuyerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuyerEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TransactionUuid = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InvoiceAuthCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    StaffId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EInvoices", x => x.EInvoiceId);
                    table.ForeignKey(
                        name: "FK_EInvoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_EInvoices_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "EInvoiceItems",
                columns: table => new
                {
                    EInvoiceItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EInvoiceId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    OrderItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EInvoiceItems", x => x.EInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_EInvoiceItems_EInvoices_EInvoiceId",
                        column: x => x.EInvoiceId,
                        principalTable: "EInvoices",
                        principalColumn: "EInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EInvoiceItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "OrderItemId");
                    table.ForeignKey(
                        name: "FK_EInvoiceItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_EInvoiceConfigs_CreatedByStaffId",
                table: "EInvoiceConfigs",
                column: "CreatedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_EInvoiceItems_EInvoiceId",
                table: "EInvoiceItems",
                column: "EInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_EInvoiceItems_OrderItemId",
                table: "EInvoiceItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EInvoiceItems_ProductId",
                table: "EInvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_EInvoices_OrderId",
                table: "EInvoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EInvoices_StaffId",
                table: "EInvoices",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "EInvoiceConfigs");

            migrationBuilder.DropTable(
                name: "EInvoiceItems");

            migrationBuilder.DropTable(
                name: "EInvoices");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems");
        }
    }
}
