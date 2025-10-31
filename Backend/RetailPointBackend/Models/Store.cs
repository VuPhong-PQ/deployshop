using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class Store
    {
        [Key]
        public int StoreId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
        
        [StringLength(50)]
        public string? TaxCode { get; set; }
        
        [StringLength(200)]
        public string? Manager { get; set; } // Tên quản lý
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual ICollection<Staff> Staffs { get; set; } = new List<Staff>();
        public virtual ICollection<StaffStore> StaffStores { get; set; } = new List<StaffStore>();
    }
}