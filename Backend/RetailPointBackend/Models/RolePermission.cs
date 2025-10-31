using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public class RolePermission
    {
        [Key]
        public int RolePermissionId { get; set; }
        
        [ForeignKey("Role")]
        public int RoleId { get; set; }
        
        [ForeignKey("Permission")]
        public int PermissionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}