using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public bool IsVisible { get; set; }
    }
}