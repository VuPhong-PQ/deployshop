using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class PaymentMethodConfig
    {
        [Key]
        public int Id { get; set; }
        public bool EnableCash { get; set; } = true;
        public bool EnableBankCard { get; set; } = true;
        public bool EnableQRCode { get; set; } = true;
        public bool EnableEWallet { get; set; } = true;
        public bool EnableBankTransfer { get; set; } = true;
        public bool EnablePartialPayment { get; set; } = false;
        public bool EnableDrawer { get; set; } = true;
        public string DefaultMethod { get; set; } = "cash";
    }
}
