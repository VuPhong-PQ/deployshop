using System;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class PaymentSettings
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string StoreId { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? QrApi { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
