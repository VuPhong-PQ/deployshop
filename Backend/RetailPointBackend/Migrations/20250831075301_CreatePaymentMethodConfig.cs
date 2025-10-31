using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class CreatePaymentMethodConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethodConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnableCash = table.Column<bool>(type: "bit", nullable: false),
                    EnableBankCard = table.Column<bool>(type: "bit", nullable: false),
                    EnableQRCode = table.Column<bool>(type: "bit", nullable: false),
                    EnableEWallet = table.Column<bool>(type: "bit", nullable: false),
                    EnableBankTransfer = table.Column<bool>(type: "bit", nullable: false),
                    EnablePartialPayment = table.Column<bool>(type: "bit", nullable: false),
                    EnableDrawer = table.Column<bool>(type: "bit", nullable: false),
                    DefaultMethod = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethodConfigs");
        }
    }
}
