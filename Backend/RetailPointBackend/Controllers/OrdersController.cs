using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using System.Linq;
using System.Text.Json;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly RetailPointContext _context;
        private readonly AppDbContext _notificationContext;
        private readonly INotificationService _notificationService;
        private readonly IDiscountService _discountService;
        
        public OrdersController(RetailPointContext context, AppDbContext notificationContext, INotificationService notificationService, IDiscountService discountService)
        {
            _context = context;
            _notificationContext = notificationContext;
            _notificationService = notificationService;
            _discountService = discountService;
        }

        [HttpPost]
        public IActionResult CreateOrder(
            [FromForm] string? orderNumber,
            [FromForm] int? customerId,
            [FromForm] int? staffId,
            [FromForm] int? storeId,
            [FromForm] string? subtotal,
            [FromForm] string? taxAmount,
            [FromForm] string? discountAmount,
            [FromForm] string? total,
            [FromForm] string? paymentMethod,
            [FromForm] string? paymentStatus,
            [FromForm] string? status)
        {
            // Lấy danh sách sản phẩm từ form-data
            var items = new List<OrderItem>();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("items[") && key.Contains("]"))
                {
                    var idxStart = key.IndexOf('[') + 1;
                    var idxEnd = key.IndexOf(']');
                    var idx = int.Parse(key.Substring(idxStart, idxEnd - idxStart));
                    while (items.Count <= idx) items.Add(new OrderItem());
                    var field = key.Substring(idxEnd + 2); // .field
                    if (field.StartsWith(".")) field = field.Substring(1); // remove leading dot
                    var value = Request.Form[key];
                    switch (field)
                    {
                        case "productId": items[idx].ProductId = int.TryParse(value, out var pid) ? pid : 0; break;
                        case "productName": items[idx].ProductName = value; break;
                        case "quantity": items[idx].Quantity = int.TryParse(value, out var qty) ? qty : 1; break;
                        case "unitPrice": items[idx].Price = decimal.TryParse(value, out var pr) ? pr : 0; break;
                        case "totalPrice": items[idx].TotalPrice = decimal.TryParse(value, out var tp) ? tp : 0; break;
                    }
                }
            }
            if (!items.Any()) return BadRequest("Order or items missing");
            
            // Nếu customerId = 0 thì set thành null (khách vãng lai)
            int? actualCustomerId = customerId.HasValue && customerId.Value > 0 ? customerId : null;
            
            var order = new Order
            {
                CustomerId = actualCustomerId,
                OrderId = 0,
                CustomerName = null,
                TotalAmount = decimal.TryParse(total, out var t) ? t : 0,
                SubTotal = decimal.TryParse(subtotal, out var st) ? st : 0,
                TaxAmount = decimal.TryParse(taxAmount, out var ta) ? ta : 0,
                DiscountAmount = decimal.TryParse(discountAmount, out var da) ? da : 0,
                PaymentMethod = paymentMethod ?? "cash",
                PaymentStatus = paymentStatus ?? "pending", // Default là pending thay vì paid
                Status = status ?? "pending", // Default là pending thay vì completed
                OrderNumber = orderNumber,
                StaffId = staffId,
                StoreId = storeId?.ToString(), // Convert int? to string
                Items = items
            };
            // Nếu có CustomerId, gán lại CustomerName từ bảng Customer
            if (order.CustomerId.HasValue && order.CustomerId > 0)
            {
                var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == order.CustomerId);
                if (customer != null)
                {
                    order.CustomerName = customer.HoTen;
                }
            }

            // Kiểm tra và trừ tồn kho cho mỗi sản phẩm trong đơn hàng
            var lowStockProducts = new List<string>();
            var insufficientStockProducts = new List<string>();

            foreach (var item in items)
            {
                var product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    // Kiểm tra xem có đủ tồn kho hay không
                    if (product.StockQuantity < item.Quantity)
                    {
                        insufficientStockProducts.Add($"{product.Name} (còn {product.StockQuantity}, cần {item.Quantity})");
                        continue;
                    }

                    // Trừ tồn kho
                    product.StockQuantity -= item.Quantity;

                    // Kiểm tra tồn kho thấp sau khi trừ
                    if (product.StockQuantity <= product.MinStockLevel)
                    {
                        lowStockProducts.Add($"{product.Name} (còn {product.StockQuantity})");
                    }
                }
            }

            // Nếu có sản phẩm không đủ tồn kho, trả về lỗi
            if (insufficientStockProducts.Any())
            {
                return BadRequest(new 
                { 
                    message = "Không đủ tồn kho cho các sản phẩm", 
                    products = insufficientStockProducts 
                });
            }

            _context.Orders.Add(order);
            _context.SaveChanges();
            
            // Tạo OrderDiscount record nếu có manual discount
            if (order.DiscountAmount > 0)
            {
                // Tìm hoặc tạo discount record cho manual discount
                var manualDiscount = _notificationContext.Discounts.FirstOrDefault(d => d.Name == "Giảm giá thủ công");
                if (manualDiscount == null)
                {
                    manualDiscount = new Discount
                    {
                        Name = "Giảm giá thủ công",
                        Description = "Giảm giá được áp dụng thủ công tại quầy",
                        Type = DiscountType.FixedAmountTotal,
                        Value = 0, // Giá trị sẽ khác nhau cho từng đơn
                        IsActive = true,
                        UsageCount = 0
                    };
                    _notificationContext.Discounts.Add(manualDiscount);
                    _notificationContext.SaveChanges();
                }
                
                var orderDiscount = new OrderDiscount
                {
                    OrderId = order.OrderId,
                    DiscountId = manualDiscount.DiscountId,
                    DiscountName = "Giảm giá thủ công",
                    DiscountType = DiscountType.FixedAmountTotal, // Manual discount default to fixed amount
                    DiscountValue = order.DiscountAmount,
                    DiscountAmount = order.DiscountAmount,
                    OrderItemId = null, // Apply to whole order
                    AppliedAt = DateTime.Now,
                    AppliedBy = staffId ?? 1 // Default staff if not provided
                };
                
                _notificationContext.OrderDiscounts.Add(orderDiscount);
                
                // Cập nhật usage count
                manualDiscount.UsageCount++;
                _notificationContext.SaveChanges();
            }
            
            // Tạo thông báo đơn hàng mới
            try
            {
                var notification = new Notification
                {
                    Type = NotificationType.NewOrder,
                    Title = "Đơn hàng mới",
                    Message = $"Khách hàng {order.CustomerName ?? "Vãng lai"} vừa đặt đơn hàng #{order.OrderId}",
                    OrderId = order.OrderId,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        CustomerName = order.CustomerName ?? "Vãng lai",
                        TotalAmount = order.TotalAmount,
                        FormattedTotal = order.TotalAmount.ToString("N0") + "đ",
                        ItemCount = items.Count
                    })
                };
                
                _notificationContext.Notifications.Add(notification);

                // Tạo thông báo cho tồn kho thấp nếu có
                if (lowStockProducts.Any())
                {
                    var lowStockNotification = new Notification
                    {
                        Type = NotificationType.LowStock,
                        Title = "Cảnh báo tồn kho thấp",
                        Message = $"Có {lowStockProducts.Count} sản phẩm đạt mức tồn kho thấp",
                        Metadata = JsonSerializer.Serialize(new
                        {
                            ProductCount = lowStockProducts.Count,
                            Products = lowStockProducts
                        })
                    };
                    
                    _notificationContext.Notifications.Add(lowStockNotification);
                }

                _notificationContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the order creation
                Console.WriteLine($"Failed to create notification: {ex.Message}");
            }

            // Trả về kết quả với thông tin tồn kho thấp nếu có
            var result = new { order.OrderId, Status = "Success" };
            if (lowStockProducts.Any())
            {
                return Ok(new 
                { 
                    order.OrderId, 
                    Status = "Success", 
                    LowStockWarning = new 
                    { 
                        Message = "Đơn hàng đã được tạo, nhưng một số sản phẩm đạt mức tồn kho thấp",
                        Products = lowStockProducts 
                    }
                });
            }
            
            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetOrders([FromQuery] int? storeId = null)
        {
            var query = _context.Orders.AsQueryable();
            
            // Filter by StoreId if provided (for multi-store support)
            if (storeId.HasValue)
            {
                query = query.Where(o => o.StoreId == storeId.Value.ToString());
            }
            
            // Lấy danh sách orders trước
            var ordersData = query
                .Select(o => new {
                    o.OrderId,
                    o.CustomerId,
                    Customer = o.Customer != null ? new {
                        o.Customer.CustomerId,
                        o.Customer.HoTen,
                        o.Customer.SoDienThoai,
                        o.Customer.Email,
                        o.Customer.DiaChi,
                        o.Customer.HangKhachHang
                    } : null,
                    o.CustomerName,
                    o.CreatedAt,
                    o.TotalAmount,
                    o.SubTotal,
                    o.TaxAmount,
                    o.DiscountAmount,
                    o.PaymentStatus,
                    o.Status,
                    o.PaymentMethod,
                    o.StoreId,
                    CashierName = "Admin", // Tạm thời hardcode vì Staff chưa có trong context
                    o.CancellationReason, // Thêm lý do hủy nếu có
                    Items = o.Items.Select(i => new {
                        i.ProductName,
                        i.Quantity,
                        i.Price,
                        i.TotalPrice
                    }).ToList()
                })
                .OrderByDescending(o => o.OrderId)
                .ToList();

            // Lấy thông tin stores từ AppDbContext
            var storeIds = ordersData.Where(o => !string.IsNullOrEmpty(o.StoreId) && int.TryParse(o.StoreId, out _))
                .Select(o => int.Parse(o.StoreId!))
                .Distinct()
                .ToList();
                
            var stores = _notificationContext.Stores
                .Where(s => storeIds.Contains(s.StoreId))
                .ToDictionary(s => s.StoreId.ToString(), s => s.Name);

            // Gắn tên store vào orders
            var orders = ordersData.Select(o => new {
                o.OrderId,
                o.CustomerId,
                o.Customer,
                o.CustomerName,
                o.CreatedAt,
                o.TotalAmount,
                o.SubTotal,
                o.TaxAmount,
                o.DiscountAmount,
                o.PaymentStatus,
                o.Status,
                o.PaymentMethod,
                o.StoreId,
                StoreName = !string.IsNullOrEmpty(o.StoreId) && stores.ContainsKey(o.StoreId) ?
                    stores[o.StoreId] : "Cửa hàng chính",
                o.CashierName,
                o.CancellationReason,
                o.Items
            }).ToList();
            
            return Ok(orders);
        }

        // Lấy chi tiết đơn hàng theo ID
        [HttpGet("{id}")]
        public IActionResult GetOrderById(int id)
        {
            var order = _context.Orders
                .Where(o => o.OrderId == id)
                .Select(o => new {
                    o.OrderId,
                    o.CustomerId,
                    Customer = o.Customer != null ? new {
                        o.Customer.CustomerId,
                        o.Customer.HoTen,
                        o.Customer.SoDienThoai,
                        o.Customer.Email,
                        o.Customer.DiaChi,
                        o.Customer.HangKhachHang
                    } : null,
                    o.CustomerName,
                    o.CreatedAt,
                    o.TotalAmount,
                    o.SubTotal,
                    o.TaxAmount,
                    o.DiscountAmount,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Status,
                    o.OrderNumber,
                    o.StaffId,
                    o.StoreId,
                    o.Notes,
                    o.CancellationReason,
                    Items = o.Items.Select(i => new {
                        i.ProductId,
                        i.ProductName,
                        i.Quantity,
                        i.Price,
                        i.TotalPrice
                    }).ToList()
                })
                .FirstOrDefault();
            
            if (order == null) return NotFound();
            
            // Lấy tên store nếu có StoreId
            string storeName = "Cửa hàng chính";
            if (!string.IsNullOrEmpty(order.StoreId) && int.TryParse(order.StoreId, out int storeId))
            {
                var store = _notificationContext.Stores.FirstOrDefault(s => s.StoreId == storeId);
                if (store != null)
                {
                    storeName = store.Name;
                }
            }
            
            var result = new {
                order.OrderId,
                order.CustomerId,
                order.Customer,
                order.CustomerName,
                order.CreatedAt,
                order.TotalAmount,
                order.SubTotal,
                order.TaxAmount,
                order.DiscountAmount,
                order.PaymentMethod,
                order.PaymentStatus,
                order.Status,
                order.OrderNumber,
                order.StaffId,
                order.StoreId,
                StoreName = storeName,
                order.Notes,
                order.CancellationReason,
                order.Items
            };
            
            return Ok(result);
        }

        // Cập nhật đơn hàng từ pending thành completed
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteOrder(int id,
            [FromForm] string? paymentMethod,
            [FromForm] string? paymentStatus,
            [FromForm] string? status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound("Không tìm thấy đơn hàng");
            
            // Cập nhật thông tin thanh toán
            order.PaymentMethod = paymentMethod ?? order.PaymentMethod;
            order.PaymentStatus = paymentStatus ?? "paid";
            order.Status = status ?? "completed";
            order.OrderNumber = $"ORD{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            
            try
            {
                _context.SaveChanges();
                
                // Tạo thông báo thanh toán thành công
                await _notificationService.CreatePaymentSuccessNotificationAsync(order.OrderId, order.TotalAmount, order.PaymentMethod ?? "cash");
                
                return Ok(new { message = "Đơn hàng đã được cập nhật thành công", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật đơn hàng", error = ex.Message });
            }
        }

        // Cập nhật đơn hàng
        [HttpPut("{id}")]
        public IActionResult UpdateOrder(int id, [FromBody] Order updatedOrder)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();
            
            // Cập nhật các field được gửi lên
            if (updatedOrder.CustomerId.HasValue) order.CustomerId = updatedOrder.CustomerId;
            if (!string.IsNullOrEmpty(updatedOrder.CustomerName)) order.CustomerName = updatedOrder.CustomerName;
            if (updatedOrder.TotalAmount > 0) order.TotalAmount = updatedOrder.TotalAmount;
            if (!string.IsNullOrEmpty(updatedOrder.Status)) order.Status = updatedOrder.Status;
            if (!string.IsNullOrEmpty(updatedOrder.PaymentStatus)) order.PaymentStatus = updatedOrder.PaymentStatus;
            if (!string.IsNullOrEmpty(updatedOrder.PaymentMethod)) order.PaymentMethod = updatedOrder.PaymentMethod;
            
            // Cập nhật lý do hủy nếu trạng thái là cancelled
            if (!string.IsNullOrEmpty(updatedOrder.CancellationReason)) 
            {
                order.CancellationReason = updatedOrder.CancellationReason;
            }
            
            Console.WriteLine($"Updating order {id}: Status = {updatedOrder.Status}, CancellationReason = {updatedOrder.CancellationReason}");
            _context.SaveChanges();
            
            return Ok(new { order.OrderId, Status = "Updated", NewStatus = order.Status, CancellationReason = order.CancellationReason });
        }

        // Xóa đơn hàng
        [HttpDelete("{id}")]
        public IActionResult DeleteOrder(int id)
        {
            try
            {
                Console.WriteLine($"Attempting to delete order {id}");
                
                // Tìm order trước
                var order = _context.Orders.Find(id);
                if (order == null) 
                {
                    Console.WriteLine($"Order {id} not found");
                    return NotFound(new { message = $"Đơn hàng #{id} không tồn tại" });
                }
                
                Console.WriteLine($"Found order {id}, deleting in correct order...");
                
                // Bước 1: Xóa Notifications liên quan đến order này trước
                var notifications = _notificationContext.Notifications.Where(n => n.OrderId == id).ToList();
                Console.WriteLine($"Found {notifications.Count} notifications to delete");
                
                if (notifications.Any())
                {
                    _notificationContext.Notifications.RemoveRange(notifications);
                    _notificationContext.SaveChanges();
                }
                
                // Bước 2: Xóa OrderItems
                var orderItems = _context.OrderItems.Where(oi => oi.OrderId == id).ToList();
                Console.WriteLine($"Found {orderItems.Count} order items to delete");
                
                if (orderItems.Any())
                {
                    _context.OrderItems.RemoveRange(orderItems);
                }
                
                // Bước 3: Cuối cùng xóa Order
                _context.Orders.Remove(order);
                _context.SaveChanges();
                
                Console.WriteLine($"Successfully deleted order {id}");
                return Ok(new { Status = "Deleted", OrderId = id, Message = $"Đã xóa đơn hàng #{id}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    message = "Lỗi khi xóa đơn hàng", 
                    error = ex.Message, 
                    orderId = id 
                });
            }
        }

        // Phương thức POST JSON để tạo đơn hàng
        [HttpPost("json")]
        public async Task<IActionResult> CreateOrderFromJson([FromBody] CreateOrderRequest request)
        {
            try
            {
                if (request?.OrderItems == null || !request.OrderItems.Any())
                {
                    return BadRequest(new { message = "Đơn hàng phải có ít nhất một sản phẩm" });
                }

                var insufficientStockProducts = new List<string>();
                var lowStockProducts = new List<string>();

                // Kiểm tra và trừ tồn kho cho từng sản phẩm
                foreach (var orderItem in request.OrderItems)
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductId);
                    if (product != null)
                    {
                        // Kiểm tra xem có đủ tồn kho hay không
                        if (product.StockQuantity < orderItem.Quantity)
                        {
                            insufficientStockProducts.Add($"{product.Name} (còn {product.StockQuantity}, cần {orderItem.Quantity})");
                            continue;
                        }

                        // Trừ tồn kho
                        product.StockQuantity -= orderItem.Quantity;

                        // Kiểm tra tồn kho thấp sau khi trừ
                        if (product.StockQuantity <= product.MinStockLevel)
                        {
                            lowStockProducts.Add($"{product.Name} (còn {product.StockQuantity})");
                        }
                    }
                }

                // Nếu có sản phẩm không đủ tồn kho, trả về lỗi
                if (insufficientStockProducts.Any())
                {
                    return BadRequest(new 
                    { 
                        message = "Không đủ tồn kho cho các sản phẩm", 
                        products = insufficientStockProducts 
                    });
                }

                // Tạo đơn hàng
                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    CustomerId = request.CustomerId,
                    TotalAmount = request.OrderItems.Sum(x => x.Quantity * x.UnitPrice),
                    SubTotal = request.OrderItems.Sum(x => x.Quantity * x.UnitPrice),
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    PaymentMethod = request.PaymentMethod ?? "cash",
                    PaymentStatus = request.PaymentStatus ?? "pending",
                    Status = request.Status ?? "pending",
                    CreatedAt = DateTime.Now,
                    StaffId = request.StaffId,
                    StoreId = request.StoreId?.ToString(), // Convert int? to string
                    Notes = request.Notes
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Thêm OrderItems
                foreach (var orderItem in request.OrderItems)
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductId);
                    var item = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = orderItem.ProductId,
                        ProductName = product?.Name ?? "Unknown",
                        Quantity = orderItem.Quantity,
                        Price = orderItem.UnitPrice,
                        TotalPrice = orderItem.Quantity * orderItem.UnitPrice
                    };
                    _context.OrderItems.Add(item);
                }

                await _context.SaveChangesAsync();

                // Tạo thông báo đơn hàng mới
                try
                {
                    var notification = new Notification
                    {
                        Type = NotificationType.NewOrder,
                        Title = "Đơn hàng mới",
                        Message = $"Khách hàng {order.CustomerName ?? "Vãng lai"} vừa đặt đơn hàng #{order.OrderId}",
                        OrderId = order.OrderId,
                        Metadata = JsonSerializer.Serialize(new
                        {
                            CustomerName = order.CustomerName ?? "Vãng lai",
                            TotalAmount = order.TotalAmount,
                            FormattedTotal = order.TotalAmount.ToString("N0") + "đ",
                            ItemCount = request.OrderItems.Count
                        })
                    };
                    
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to create order notification: {ex.Message}");
                }

                // Tạo thông báo tồn kho thấp nếu có
                if (lowStockProducts.Any())
                {
                    try
                    {
                        var lowStockNotification = new Notification
                        {
                            Type = NotificationType.LowStock,
                            Title = "Cảnh báo tồn kho thấp",
                            Message = $"Các sản phẩm sau có tồn kho thấp: {string.Join(", ", lowStockProducts)}",
                            Metadata = JsonSerializer.Serialize(new
                            {
                                LowStockProducts = lowStockProducts,
                                Count = lowStockProducts.Count
                            })
                        };
                        
                        _context.Notifications.Add(lowStockNotification);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to create low stock notification: {ex.Message}");
                    }
                }

                return Ok(new 
                { 
                    message = "Đơn hàng được tạo thành công",
                    orderId = order.OrderId,
                    totalAmount = order.TotalAmount,
                    lowStockWarnings = lowStockProducts.Any() ? lowStockProducts : null
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating order: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    message = "Lỗi khi tạo đơn hàng", 
                    error = ex.Message 
                });
            }
        }
    }

    // DTO classes for JSON requests
    public class CreateOrderRequest
    {
        public string? CustomerName { get; set; }
        public int? CustomerId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Status { get; set; }
        public int? StaffId { get; set; }
        public int? StoreId { get; set; }
        public string? Notes { get; set; }
        public List<CreateOrderItemRequest> OrderItems { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
