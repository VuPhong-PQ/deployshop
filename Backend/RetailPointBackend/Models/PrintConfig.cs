using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class PrintConfig
    {
        [Key]
        public int Id { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public string PaperSize { get; set; } = "80mm";
        public int PrintCopies { get; set; } = 1;
        public bool AutoPrintBill { get; set; } = true;
        public bool AutoPrintOnOrder { get; set; } = false;
        public bool PrintBarcode { get; set; } = true;
        public bool PrintLogo { get; set; } = true;
        public string BillHeader { get; set; } = string.Empty;
        public string BillFooter { get; set; } = string.Empty;
    }
}
