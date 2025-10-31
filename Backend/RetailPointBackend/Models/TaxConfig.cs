using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class TaxConfig
    {
        [Key]
        public int Id { get; set; }
        public bool EnableVAT { get; set; } = false;
        public bool VATIncludedInPrice { get; set; } = false;
        public decimal VATRate { get; set; } = 0;
        public string VATLabel { get; set; } = "VAT";
        public bool EnableEnvTax { get; set; } = false;
        public decimal EnvTaxRate { get; set; } = 0;
    }
}
