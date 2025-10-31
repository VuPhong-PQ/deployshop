using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class ProductGroup
    {
        [Key]
        public int ProductGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    // Đã loại bỏ các trường: mô tả, màu sắc, thứ tự, cho phép hiển thị
    }
}
