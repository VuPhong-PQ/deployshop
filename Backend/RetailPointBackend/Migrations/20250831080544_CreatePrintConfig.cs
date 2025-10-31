using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class CreatePrintConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrintConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrinterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaperSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrintCopies = table.Column<int>(type: "int", nullable: false),
                    AutoPrintBill = table.Column<bool>(type: "bit", nullable: false),
                    AutoPrintOnOrder = table.Column<bool>(type: "bit", nullable: false),
                    PrintBarcode = table.Column<bool>(type: "bit", nullable: false),
                    PrintLogo = table.Column<bool>(type: "bit", nullable: false),
                    BillHeader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillFooter = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintConfigs");
        }
    }
}
