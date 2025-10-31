using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PermissionName { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        [StringLength(100)]
        public string? Category { get; set; } // Ví dụ: "Sales", "Admin", "Reports"
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}