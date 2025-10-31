using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Models;
using System.Linq;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderItemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public OrderItemsController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả OrderItem của 1 đơn hàng
        [HttpGet("order/{orderId}")]
        public IActionResult GetItemsByOrder(int orderId)
        {
            var items = _context.OrderItems.Where(i => i.OrderId == orderId).ToList();
            return Ok(items);
        }

        // Thêm mới 1 OrderItem
        [HttpPost]
        public IActionResult AddOrderItem([FromBody] OrderItem item)
        {
            _context.OrderItems.Add(item);
            _context.SaveChanges();
            return Ok(new { item.OrderItemId, Status = "Success" });
        }

        // Cập nhật 1 OrderItem
        [HttpPut("{id}")]
        public IActionResult UpdateOrderItem(int id, [FromBody] OrderItem updatedItem)
        {
            var item = _context.OrderItems.FirstOrDefault(i => i.OrderItemId == id);
            if (item == null) return NotFound();
            item.ProductId = updatedItem.ProductId;
            item.ProductName = updatedItem.ProductName;
            item.Quantity = updatedItem.Quantity;
            item.Price = updatedItem.Price;
            item.TotalPrice = updatedItem.TotalPrice;
            _context.SaveChanges();
            return Ok(new { item.OrderItemId, Status = "Updated" });
        }

        // Xóa 1 OrderItem
        [HttpDelete("{id}")]
        public IActionResult DeleteOrderItem(int id)
        {
            var item = _context.OrderItems.FirstOrDefault(i => i.OrderItemId == id);
            if (item == null) return NotFound();
            _context.OrderItems.Remove(item);
            _context.SaveChanges();
            return Ok(new { Status = "Deleted" });
        }
    }
}