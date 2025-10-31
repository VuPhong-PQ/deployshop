using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class BackupHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime BackupDate { get; set; }

        [Required]
        [StringLength(50)]
        public string BackupType { get; set; } = string.Empty; // "Manual", "Auto"

        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        public double FileSizeMB { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty; // "Success", "Failed"

        [StringLength(1000)]
        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}