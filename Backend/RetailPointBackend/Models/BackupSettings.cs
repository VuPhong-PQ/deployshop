using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class BackupSettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public TimeSpan BackupTime { get; set; } = new TimeSpan(13, 0, 0); // Default 13:00
        
        public bool IsEnabled { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}