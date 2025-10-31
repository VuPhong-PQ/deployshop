using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Text.Json;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetNotifications(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? unreadOnly = null)
        {
            var query = _context.Notifications.AsQueryable();

            if (unreadOnly == true)
            {
                query = query.Where(n => n.Status == NotificationStatus.Unread);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = notifications.Select(n => new
            {
                n.NotificationId,
                n.Type,
                n.Title,
                n.Message,
                n.Status,
                n.CreatedAt,
                n.ReadAt,
                n.OrderId,
                n.ProductId,
                n.CustomerId,
                Metadata = n.Metadata != null ? JsonSerializer.Deserialize<object>(n.Metadata) : null
            }).ToList();

            return Ok(result);
        }

        // GET: api/notifications/count
        [HttpGet("count")]
        public async Task<ActionResult<object>> GetNotificationCount()
        {
            var unreadCount = await _context.Notifications
                .CountAsync(n => n.Status == NotificationStatus.Unread);

            var totalCount = await _context.Notifications.CountAsync();

            return Ok(new { unreadCount, totalCount });
        }

        // POST: api/notifications/{id}/mark-read
        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/notifications/mark-all-read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.Status == NotificationStatus.Unread)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var notification in unreadNotifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = now;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/notifications/{id}/navigate
        [HttpGet("{id}/navigate")]
        public async Task<IActionResult> GetNavigationInfo(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            // Mark as read if unread
            if (notification.Status == NotificationStatus.Unread)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Return navigation info based on notification type
            object navigationInfo = notification.Type switch
            {
                NotificationType.NewOrder => new
                {
                    type = "order",
                    path = "/orders",
                    orderId = notification.OrderId,
                    title = "Chi tiết đơn hàng",
                    data = new
                    {
                        orderId = notification.OrderId
                    }
                },
                NotificationType.LowStock or NotificationType.OutOfStock => await GetProductNavigationInfo(notification),
                NotificationType.PaymentSuccess => new
                {
                    type = "order",
                    path = "/orders",
                    orderId = notification.OrderId,
                    title = "Chi tiết thanh toán",
                    data = new
                    {
                        orderId = notification.OrderId
                    }
                },
                _ => new
                {
                    type = "general",
                    path = "/",
                    title = "Trang chủ",
                    data = new { }
                }
            };

            return Ok(navigationInfo);
        }

        private async Task<object> GetProductNavigationInfo(Notification notification)
        {
            string? productName = null;
            int? productId = notification.ProductId;

            // Try to get product name from metadata first
            productName = GetProductNameFromMetadata(notification.Metadata);
            
            // If we have productId but no product name from metadata, fetch from database
            if (productId.HasValue && string.IsNullOrEmpty(productName))
            {
                var product = await _context.Products.FindAsync(productId.Value);
                productName = product?.Name; // Correct field name
            }
            
            // If still no product name and we have metadata, try to extract from LowStockProducts array
            if (string.IsNullOrEmpty(productName))
            {
                var productFromMetadata = GetFirstProductFromLowStockMetadata(notification.Metadata);
                if (productFromMetadata.HasValue)
                {
                    productName = productFromMetadata.Value.Name;
                    if (!productId.HasValue)
                        productId = productFromMetadata.Value.Id;
                }
            }

            return new
            {
                type = "product",
                path = "/inventory",
                productId = productId,
                title = "Chi tiết sản phẩm",
                data = new
                {
                    productId = productId,
                    searchTerm = productName ?? "Sản phẩm"
                }
            };
        }

        private (int? Id, string? Name)? GetFirstProductFromLowStockMetadata(string? metadata)
        {
            if (string.IsNullOrEmpty(metadata))
                return null;

            try
            {
                using var metadataObj = JsonSerializer.Deserialize<JsonDocument>(metadata);
                if (metadataObj?.RootElement.TryGetProperty("LowStockProducts", out var lowStockElement) == true && 
                    lowStockElement.ValueKind == JsonValueKind.Array && 
                    lowStockElement.GetArrayLength() > 0)
                {
                    var firstProduct = lowStockElement[0];
                    if (firstProduct.ValueKind == JsonValueKind.Object)
                    {
                        int? id = null;
                        string? name = null;
                        
                        if (firstProduct.TryGetProperty("ProductId", out var idElement))
                        {
                            id = idElement.GetInt32();
                        }
                        if (firstProduct.TryGetProperty("Name", out var nameElement))
                        {
                            name = nameElement.GetString();
                        }
                        
                        return (id, name);
                    }
                    // Fallback for simple string array
                    else if (firstProduct.ValueKind == JsonValueKind.String)
                    {
                        return (null, firstProduct.GetString());
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }

            return null;
        }

        private string? GetProductNameFromMetadata(string? metadata)
        {
            if (string.IsNullOrEmpty(metadata))
                return null;

            try
            {
                using var metadataObj = JsonSerializer.Deserialize<JsonDocument>(metadata);
                
                // Try to get from Products array (current format)
                if (metadataObj?.RootElement.TryGetProperty("Products", out var productsElement) == true && 
                    productsElement.ValueKind == JsonValueKind.Array && 
                    productsElement.GetArrayLength() > 0)
                {
                    var firstProduct = productsElement[0].GetString();
                    if (!string.IsNullOrEmpty(firstProduct))
                    {
                        // Extract product name from "Dép nam (còn 2)" format
                        var nameMatch = System.Text.RegularExpressions.Regex.Match(firstProduct, @"^(.+?)\s*\(");
                        return nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : firstProduct;
                    }
                }
                
                // Fallback: Try to get from ProductName property
                if (metadataObj?.RootElement.TryGetProperty("ProductName", out var productNameElement) == true)
                {
                    return productNameElement.GetString();
                }
                
                // Fallback: Try to get from LowStockProducts array (old format)
                if (metadataObj?.RootElement.TryGetProperty("LowStockProducts", out var lowStockElement) == true && 
                    lowStockElement.ValueKind == JsonValueKind.Array && 
                    lowStockElement.GetArrayLength() > 0)
                {
                    return lowStockElement[0].GetString();
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }

            return null;
        }

        // Internal method to create notifications (used by other controllers)
        public static async Task<Notification> CreateNotificationAsync(
            AppDbContext context,
            NotificationType type,
            string title,
            string? message = null,
            int? orderId = null,
            int? productId = null,
            int? customerId = null,
            object? metadata = null)
        {
            var notification = new Notification
            {
                Type = type,
                Title = title,
                Message = message,
                OrderId = orderId,
                ProductId = productId,
                CustomerId = customerId,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            return notification;
        }
    }
}