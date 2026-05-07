using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RetailPointBackend.Models;
using System.Linq;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderItemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public OrderItemsController(AppDbContext context)
        {
            _context = context;
        }

        // Láº¥y táº¥t cáº£ OrderItem cá»§a 1 Ä‘Æ¡n hÃ ng
        [HttpGet("order/{orderId}")]
        public IActionResult GetItemsByOrder(int orderId)
        {
            var items = _context.OrderItems.Where(i => i.OrderId == orderId).ToList();
            return Ok(items);
        }

        // ThÃªm má»›i 1 OrderItem
        [HttpPost]
        public IActionResult AddOrderItem([FromBody] OrderItem item)
        {
            _context.OrderItems.Add(item);
            _context.SaveChanges();
            return Ok(new { item.OrderItemId, Status = "Success" });
        }

        // Cáº­p nháº­t 1 OrderItem
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

        // XÃ³a 1 OrderItem
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
