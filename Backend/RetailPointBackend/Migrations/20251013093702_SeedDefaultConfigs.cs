using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailPointBackend.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "VATRate",
                table: "TaxConfigs",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EnvTaxRate",
                table: "TaxConfigs",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.InsertData(
                table: "PrintConfigs",
                columns: new[] { "Id", "AutoPrintBill", "AutoPrintOnOrder", "BillFooter", "BillHeader", "PaperSize", "PrintBarcode", "PrintCopies", "PrintLogo", "PrinterName" },
                values: new object[] { 1, true, false, "Cảm ơn quý khách!", "RETAIL POINT STORE", "80mm", true, 1, false, "Default Printer" });

            migrationBuilder.InsertData(
                table: "TaxConfigs",
                columns: new[] { "Id", "EnableEnvTax", "EnableVAT", "EnvTaxRate", "VATIncludedInPrice", "VATLabel", "VATRate" },
                values: new object[] { 1, false, false, 2.0m, true, "VAT", 10.0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PrintConfigs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TaxConfigs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "VATRate",
                table: "TaxConfigs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "EnvTaxRate",
                table: "TaxConfigs",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
