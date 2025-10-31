using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public class StaffStore
    {
        [Key]
        public int StaffStoreId { get; set; }
        
        [ForeignKey("Staff")]
        public int StaffId { get; set; }
        
        [ForeignKey("Store")]
        public int StoreId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Staff Staff { get; set; } = null!;
        public virtual Store Store { get; set; } = null!;
    }
}