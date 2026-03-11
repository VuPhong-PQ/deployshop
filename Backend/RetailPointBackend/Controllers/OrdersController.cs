using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IDiscountService _discountService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<OrdersController> _logger;
        
        public OrdersController(AppDbContext context, INotificationService notificationService, IDiscountService discountService, ILoyaltyService loyaltyService, ILogger<OrdersController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _discountService = discountService;
            _loyaltyService = loyaltyService;
            _logger = logger;
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
            [FromForm] string? status,
            [FromForm] string? createdAt,
            [FromForm] string? currency)
        {
            // Láº¥y danh sÃ¡ch sáº£n pháº©m tá»« form-data
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
            
            // Náº¿u customerId = 0 thÃ¬ set thÃ nh null (khÃ¡ch vÃ£ng lai)
            int? actualCustomerId = customerId.HasValue && customerId.Value > 0 ? customerId : null;
            
            var order = new Order
            {
                CustomerId = actualCustomerId,
                OrderId = 0,
                // CreatedAt will default to DateTime.Now, but if client provided a createdAt value
                // (including timezone offset), parse and set it below.
                CustomerName = null,
                TotalAmount = decimal.TryParse(total, out var t) ? t : 0,
                SubTotal = decimal.TryParse(subtotal, out var st) ? st : 0,
                TaxAmount = decimal.TryParse(taxAmount, out var ta) ? ta : 0,
                DiscountAmount = decimal.TryParse(discountAmount, out var da) ? da : 0,
                PaymentMethod = paymentMethod ?? "cash",
                PaymentStatus = paymentStatus ?? "pending", // Default lÃ  pending thay vÃ¬ paid
                Status = status ?? "pending", // Default lÃ  pending thay vÃ¬ completed
                OrderNumber = orderNumber,
                StaffId = staffId,
                StoreId = storeId?.ToString(), // Convert int? to string
                Items = items
            };

            // If client provided a createdAt value, try to parse it (supporting ISO with offset)
            if (!string.IsNullOrEmpty(createdAt))
            {
                try
                {
                    // Prefer DateTimeOffset to preserve wall-clock time when an offset is present
                    if (DateTimeOffset.TryParse(createdAt, out var dto))
                    {
                        // Use DateTime with the same wall-clock values (Kind = Unspecified)
                        order.CreatedAt = dto.DateTime;
                    }
                    else if (DateTime.TryParse(createdAt, out var dt))
                    {
                        order.CreatedAt = dt;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse createdAt value '{createdAt}'", createdAt);
                    // leave default CreatedAt (DateTime.Now)
                }
            }
            // Náº¿u cÃ³ CustomerId, gÃ¡n láº¡i CustomerName (khÃ´ng tá»± Ä‘á»™ng Ã¡p dá»¥ng giáº£m giÃ¡)
            if (order.CustomerId.HasValue && order.CustomerId > 0)
            {
                var customer = _context.Customers
                    .Include(c => c.CustomerTier)
                    .FirstOrDefault(c => c.CustomerId == order.CustomerId);
                if (customer != null)
                {
                    order.CustomerName = customer.HoTen;
                    
                    // KHÃ”NG tá»± Ä‘á»™ng Ã¡p dá»¥ng giáº£m giÃ¡ theo háº¡ng khÃ¡ch hÃ ng
                    // Giáº£m giÃ¡ sáº½ chá»‰ Ä‘Æ°á»£c Ã¡p dá»¥ng khi frontend gá»­i lÃªn rÃµ rÃ ng
                    // hoáº·c thÃ´ng qua há»‡ thá»‘ng discount selector
                    
                    _logger.LogInformation("Order for customer {CustomerId} ({CustomerName}) - Tier: {TierName}. Discount will only be applied if explicitly selected.", 
                        customer.CustomerId, customer.HoTen, customer.CustomerTier?.TierName ?? "None");
                }
            }

            // Kiá»ƒm tra vÃ  trá»« tá»“n kho cho má»—i sáº£n pháº©m trong Ä‘Æ¡n hÃ ng
            var lowStockProducts = new List<string>();
            var insufficientStockProducts = new List<string>();

            foreach (var item in items)
            {
                var product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    // Kiá»ƒm tra xem cÃ³ Ä‘á»§ tá»“n kho hay khÃ´ng
                    if (product.StockQuantity < item.Quantity)
                    {
                        insufficientStockProducts.Add($"{product.Name} (cÃ²n {product.StockQuantity}, cáº§n {item.Quantity})");
                        continue;
                    }

                    // Trá»« tá»“n kho
                    product.StockQuantity -= item.Quantity;

                    // Kiá»ƒm tra tá»“n kho tháº¥p sau khi trá»«
                    if (product.StockQuantity <= product.MinStockLevel)
                    {
                        lowStockProducts.Add($"{product.Name} (cÃ²n {product.StockQuantity})");
                    }
                }
            }

            // Náº¿u cÃ³ sáº£n pháº©m khÃ´ng Ä‘á»§ tá»“n kho, tráº£ vá» lá»—i
            if (insufficientStockProducts.Any())
            {
                return BadRequest(new 
                { 
                    message = "KhÃ´ng Ä‘á»§ tá»“n kho cho cÃ¡c sáº£n pháº©m", 
                    products = insufficientStockProducts 
                });
            }

            _context.Orders.Add(order);
            _context.SaveChanges();
            
            // Táº¡o OrderDiscount record náº¿u cÃ³ manual discount
            if (order.DiscountAmount > 0)
            {
                // TÃ¬m hoáº·c táº¡o discount record cho manual discount
                var manualDiscount = _context.Discounts.FirstOrDefault(d => d.Name == "Giáº£m giÃ¡ thá»§ cÃ´ng");
                if (manualDiscount == null)
                {
                    manualDiscount = new Discount
                    {
                        Name = "Giáº£m giÃ¡ thá»§ cÃ´ng",
                        Description = "Giáº£m giÃ¡ Ä‘Æ°á»£c Ã¡p dá»¥ng thá»§ cÃ´ng táº¡i quáº§y",
                        Type = DiscountType.FixedAmountTotal,
                        Value = 0, // GiÃ¡ trá»‹ sáº½ khÃ¡c nhau cho tá»«ng Ä‘Æ¡n
                        IsActive = true,
                        UsageCount = 0
                    };
                    _context.Discounts.Add(manualDiscount);
                    _context.SaveChanges();
                }
                
                var orderDiscount = new OrderDiscount
                {
                    OrderId = order.OrderId,
                    DiscountId = manualDiscount.DiscountId,
                    DiscountName = "Giáº£m giÃ¡ thá»§ cÃ´ng",
                    DiscountType = DiscountType.FixedAmountTotal, // Manual discount default to fixed amount
                    DiscountValue = order.DiscountAmount,
                    DiscountAmount = order.DiscountAmount,
                    OrderItemId = null, // Apply to whole order
                    AppliedAt = DateTime.Now,
                    AppliedBy = staffId ?? 1 // Default staff if not provided
                };
                
                _context.OrderDiscounts.Add(orderDiscount);
                
                // Cáº­p nháº­t usage count
                manualDiscount.UsageCount++;
                _context.SaveChanges();
            }
            
            // Táº¡o thÃ´ng bÃ¡o Ä‘Æ¡n hÃ ng má»›i
            try
            {
                var notification = new Notification
                {
                    Type = NotificationType.NewOrder,
                    Title = "ÄÆ¡n hÃ ng má»›i",
                    Message = $"KhÃ¡ch hÃ ng {order.CustomerName ?? "VÃ£ng lai"} vá»«a Ä‘áº·t Ä‘Æ¡n hÃ ng #{order.OrderId}",
                    OrderId = order.OrderId,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        CustomerName = order.CustomerName ?? "VÃ£ng lai",
                        TotalAmount = order.TotalAmount,
                        FormattedTotal = order.TotalAmount.ToString("N0") + "Ä‘",
                        ItemCount = items.Count
                    })
                };
                
                _context.Notifications.Add(notification);

                // Táº¡o thÃ´ng bÃ¡o cho tá»“n kho tháº¥p náº¿u cÃ³
                if (lowStockProducts.Any())
                {
                    var lowStockNotification = new Notification
                    {
                        Type = NotificationType.LowStock,
                        Title = "Cáº£nh bÃ¡o tá»“n kho tháº¥p",
                        Message = $"CÃ³ {lowStockProducts.Count} sáº£n pháº©m Ä‘áº¡t má»©c tá»“n kho tháº¥p",
                        Metadata = JsonSerializer.Serialize(new
                        {
                            ProductCount = lowStockProducts.Count,
                            Products = lowStockProducts
                        })
                    };
                    
                    _context.Notifications.Add(lowStockNotification);
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the order creation
                Console.WriteLine($"Failed to create notification: {ex.Message}");
            }

            // Xá»­ lÃ½ tÃ­ch Ä‘iá»ƒm vÃ  nÃ¢ng háº¡ng cho khÃ¡ch hÃ ng (chá»‰ khi cÃ³ CustomerId)
            if (actualCustomerId.HasValue)
            {
                try
                {
                    // TÃ­ch Ä‘iá»ƒm cho Ä‘Æ¡n hÃ ng (cháº¡y background Ä‘á»ƒ khÃ´ng áº£nh hÆ°á»Ÿng tá»‘c Ä‘á»™ táº¡o Ä‘Æ¡n)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("Processing loyalty points for order {OrderId}, customer {CustomerId}", order.OrderId, actualCustomerId.Value);
                            var pointsProcessed = await _loyaltyService.ProcessOrderPointsAsync(order.OrderId);
                            if (pointsProcessed)
                            {
                                _logger.LogInformation("Loyalty points processed successfully for order {OrderId}", order.OrderId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to process loyalty points for order {OrderId}", order.OrderId);
                            }
                        }
                        catch (Exception loyaltyEx)
                        {
                            _logger.LogError(loyaltyEx, "Error processing loyalty points for order {OrderId}", order.OrderId);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting loyalty processing task for order {OrderId}", order.OrderId);
                }
            }

            // Tráº£ vá» káº¿t quáº£ vá»›i thÃ´ng tin tá»“n kho tháº¥p náº¿u cÃ³
            var result = new { order.OrderId, Status = "Success" };
            if (lowStockProducts.Any())
            {
                return Ok(new 
                { 
                    order.OrderId, 
                    Status = "Success", 
                    LowStockWarning = new 
                    { 
                        Message = "ÄÆ¡n hÃ ng Ä‘Ã£ Ä‘Æ°á»£c táº¡o, nhÆ°ng má»™t sá»‘ sáº£n pháº©m Ä‘áº¡t má»©c tá»“n kho tháº¥p",
                        Products = lowStockProducts 
                    }
                });
            }
            
            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetOrders(
            [FromQuery] int? storeId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // pageSize = 0 => return all
            if (page < 1) page = 1;

            var query = _context.Orders.AsQueryable();

            // Filter by StoreId if provided (for multi-store support)
            if (storeId.HasValue)
            {
                query = query.Where(o => o.StoreId == storeId.Value.ToString());
            }

            // Filter by date range if provided. We treat startDate/endDate as inclusive.
            if (startDate.HasValue)
            {
                var sd = startDate.Value.Date;
                query = query.Where(o => o.CreatedAt >= sd);
            }
            if (endDate.HasValue)
            {
                var ed = endDate.Value.Date.AddDays(1).AddTicks(-1); // end of day
                query = query.Where(o => o.CreatedAt <= ed);
            }

            // Compose base projection
            var baseQuery = query
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
                    CashierName = "Admin",
                    o.CancellationReason,
                    Items = o.Items.Select(i => new {
                        i.ProductName,
                        i.Quantity,
                        i.Price,
                        i.TotalPrice
                    }).ToList()
                })
                .OrderByDescending(o => o.OrderId);

            // Get total count before pagination
            var totalCount = baseQuery.Count();

            // Apply pagination
            List<object> pageItems;
            if (pageSize <= 0)
            {
                pageItems = baseQuery.ToList<object>();
            }
            else
            {
                pageItems = baseQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList<object>();
            }

            // Resolve store names for returned items
            var storeIds = pageItems
                .Where(o => o.GetType().GetProperty("StoreId") != null)
                .Select(o => {
                    var val = o.GetType().GetProperty("StoreId")!.GetValue(o)?.ToString();
                    return int.TryParse(val, out var id) ? (int?)id : null;
                })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var stores = _context.Stores
                .Where(s => storeIds.Contains(s.StoreId))
                .ToDictionary(s => s.StoreId.ToString(), s => s.Name);

            var orders = pageItems.Select(o => {
                var storeIdProp = o.GetType().GetProperty("StoreId")!.GetValue(o)?.ToString();
                var storeName = !string.IsNullOrEmpty(storeIdProp) && stores.ContainsKey(storeIdProp) ? stores[storeIdProp] : "Cửa hàng chính";
                return new {
                    Order = o,
                    StoreName = storeName
                };
            }).ToList();

            var totalPages = pageSize <= 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new {
                items = orders.Select(x => new {
                    // unwrap order properties with explicit names
                    OrderId = x.Order.GetType().GetProperty("OrderId")!.GetValue(x.Order),
                    CustomerId = x.Order.GetType().GetProperty("CustomerId")!.GetValue(x.Order),
                    Customer = x.Order.GetType().GetProperty("Customer")!.GetValue(x.Order),
                    CustomerName = x.Order.GetType().GetProperty("CustomerName")!.GetValue(x.Order),
                    CreatedAt = x.Order.GetType().GetProperty("CreatedAt")!.GetValue(x.Order),
                    TotalAmount = x.Order.GetType().GetProperty("TotalAmount")!.GetValue(x.Order),
                    SubTotal = x.Order.GetType().GetProperty("SubTotal")!.GetValue(x.Order),
                    TaxAmount = x.Order.GetType().GetProperty("TaxAmount")!.GetValue(x.Order),
                    DiscountAmount = x.Order.GetType().GetProperty("DiscountAmount")!.GetValue(x.Order),
                    PaymentStatus = x.Order.GetType().GetProperty("PaymentStatus")!.GetValue(x.Order),
                    Status = x.Order.GetType().GetProperty("Status")!.GetValue(x.Order),
                    PaymentMethod = x.Order.GetType().GetProperty("PaymentMethod")!.GetValue(x.Order),
                    StoreId = x.Order.GetType().GetProperty("StoreId")!.GetValue(x.Order),
                    StoreName = x.StoreName,
                    CashierName = x.Order.GetType().GetProperty("CashierName")!.GetValue(x.Order),
                    CancellationReason = x.Order.GetType().GetProperty("CancellationReason")!.GetValue(x.Order),
                    Items = x.Order.GetType().GetProperty("Items")!.GetValue(x.Order)
                }),
                pagination = new {
                    total = totalCount,
                    page = page,
                    pageSize = pageSize <= 0 ? totalCount : pageSize,
                    totalPages = totalPages
                }
            });
        }

        // Láº¥y chi tiáº¿t Ä‘Æ¡n hÃ ng theo ID
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
            
            // Láº¥y tÃªn store náº¿u cÃ³ StoreId
            string storeName = "Cá»­a hÃ ng chÃ­nh";
            if (!string.IsNullOrEmpty(order.StoreId) && int.TryParse(order.StoreId, out int storeId))
            {
                var store = _context.Stores.FirstOrDefault(s => s.StoreId == storeId);
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

        // Cáº­p nháº­t Ä‘Æ¡n hÃ ng tá»« pending thÃ nh completed
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteOrder(int id,
            [FromForm] string? paymentMethod,
            [FromForm] string? paymentStatus,
            [FromForm] string? status,
            [FromForm] string? currency)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng");
            
            var oldStatus = order.Status;
            
            // Cáº­p nháº­t thÃ´ng tin thanh toÃ¡n
            order.PaymentMethod = paymentMethod ?? order.PaymentMethod;
            order.PaymentStatus = paymentStatus ?? "paid";
            order.Status = status ?? "completed";
            order.Currency = currency ?? order.Currency;
            order.OrderNumber = $"ORD{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            
            try
            {
                _context.SaveChanges();
                
                // Táº¡o thÃ´ng bÃ¡o thanh toÃ¡n thÃ nh cÃ´ng
                await _notificationService.CreatePaymentSuccessNotificationAsync(order.OrderId, order.TotalAmount, order.PaymentMethod ?? "cash");
                
                // Xá»­ lÃ½ tÃ­ch Ä‘iá»ƒm khi Ä‘Æ¡n hÃ ng chuyá»ƒn sang completed
                if (oldStatus != "completed" && order.Status == "completed" && order.CustomerId.HasValue)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("Processing loyalty points for completed order {OrderId}, customer {CustomerId}", order.OrderId, order.CustomerId.Value);
                            var pointsProcessed = await _loyaltyService.ProcessOrderPointsAsync(order.OrderId);
                            if (pointsProcessed)
                            {
                                _logger.LogInformation("Loyalty points processed successfully for completed order {OrderId}", order.OrderId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to process loyalty points for completed order {OrderId}", order.OrderId);
                            }
                            
                            // CRITICAL FIX: Äáº£m báº£o khÃ´ng cÃ³ discount tá»± Ä‘á»™ng nÃ o Ä‘Æ°á»£c Ã¡p dá»¥ng
                            // sau khi xá»­ lÃ½ tÃ­ch Ä‘iá»ƒm, chá» má»™t khoáº£ng thá»i gian Ä‘á»ƒ cÃ¡c process khÃ¡c hoÃ n thÃ nh 
                            await Task.Delay(3000); // Chá» 3 giÃ¢y Ä‘á»ƒ cÃ¡c background job hoÃ n thÃ nh
                            
                            // Reload order tá»« database Ä‘á»ƒ kiá»ƒm tra cÃ³ discount tá»± Ä‘á»™ng nÃ o Ä‘Æ°á»£c Ã¡p dá»¥ng khÃ´ng
                            var orderToCheck = await _context.Orders.FindAsync(order.OrderId);
                            if (orderToCheck != null && orderToCheck.DiscountAmount > 0)
                            {
                                // Kiá»ƒm tra xem cÃ³ discount record rÃµ rÃ ng nÃ o Ä‘Æ°á»£c táº¡o khÃ´ng
                                var hasExplicitDiscount = await _context.OrderDiscounts
                                    .AnyAsync(od => od.OrderId == order.OrderId);
                                
                                if (!hasExplicitDiscount)
                                {
                                    // KhÃ´ng cÃ³ discount rÃµ rÃ ng Ä‘Æ°á»£c chá»n, reset vá» 0
                                    var originalDiscountAmount = orderToCheck.DiscountAmount;
                                    orderToCheck.DiscountAmount = 0;
                                    orderToCheck.TotalAmount = orderToCheck.SubTotal + orderToCheck.TaxAmount;
                                    await _context.SaveChangesAsync();
                                    _logger.LogWarning("AUTO-DISCOUNT PREVENTION: Reset automatic discount of {DiscountAmount} for order {OrderId} as no explicit discount was selected", 
                                        originalDiscountAmount, order.OrderId);
                                }
                                else
                                {
                                    _logger.LogInformation("Order {OrderId} has explicit discount records, keeping discount amount: {DiscountAmount}", 
                                        order.OrderId, orderToCheck.DiscountAmount);
                                }
                            }
                        }
                        catch (Exception loyaltyEx)
                        {
                            _logger.LogError(loyaltyEx, "Error processing loyalty points for completed order {OrderId}", order.OrderId);
                        }
                    });
                }
                
                return Ok(new { message = "ÄÆ¡n hÃ ng Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t thÃ nh cÃ´ng", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi cáº­p nháº­t Ä‘Æ¡n hÃ ng", error = ex.Message });
            }
        }

        // Cáº­p nháº­t Ä‘Æ¡n hÃ ng
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order updatedOrder)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();
            
            var oldStatus = order.Status;
            
            // Cáº­p nháº­t cÃ¡c field Ä‘Æ°á»£c gá»­i lÃªn
            if (updatedOrder.CustomerId.HasValue) order.CustomerId = updatedOrder.CustomerId;
            if (!string.IsNullOrEmpty(updatedOrder.CustomerName)) order.CustomerName = updatedOrder.CustomerName;
            if (updatedOrder.TotalAmount > 0) order.TotalAmount = updatedOrder.TotalAmount;
            if (!string.IsNullOrEmpty(updatedOrder.Status)) order.Status = updatedOrder.Status;
            if (!string.IsNullOrEmpty(updatedOrder.PaymentStatus)) order.PaymentStatus = updatedOrder.PaymentStatus;
            if (!string.IsNullOrEmpty(updatedOrder.PaymentMethod)) order.PaymentMethod = updatedOrder.PaymentMethod;
            
            // Cáº­p nháº­t lÃ½ do há»§y náº¿u tráº¡ng thÃ¡i lÃ  cancelled
            if (!string.IsNullOrEmpty(updatedOrder.CancellationReason)) 
            {
                order.CancellationReason = updatedOrder.CancellationReason;
            }
            
            Console.WriteLine($"Updating order {id}: Status = {updatedOrder.Status}, CancellationReason = {updatedOrder.CancellationReason}");
            _context.SaveChanges();
            
            // Xá»­ lÃ½ tÃ­ch Ä‘iá»ƒm khi Ä‘Æ¡n hÃ ng Ä‘Æ°á»£c hoÃ n thÃ nh hoáº·c há»§y
            if (order.CustomerId.HasValue)
            {
                if (oldStatus != "completed" && order.Status == "completed")
                {
                    // ÄÆ¡n hÃ ng má»›i hoÃ n thÃ nh - tÃ­ch Ä‘iá»ƒm
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("Processing loyalty points for order {OrderId} status change to completed", order.OrderId);
                            await _loyaltyService.ProcessOrderPointsAsync(order.OrderId);
                            
                            // CRITICAL FIX: Äáº£m báº£o khÃ´ng cÃ³ discount tá»± Ä‘á»™ng nÃ o Ä‘Æ°á»£c Ã¡p dá»¥ng
                            await Task.Delay(3000); // Chá» 3 giÃ¢y Ä‘á»ƒ cÃ¡c background job hoÃ n thÃ nh
                            
                            var orderToCheck = await _context.Orders.FindAsync(order.OrderId);
                            if (orderToCheck != null && orderToCheck.DiscountAmount > 0)
                            {
                                var hasExplicitDiscount = await _context.OrderDiscounts
                                    .AnyAsync(od => od.OrderId == order.OrderId);
                                
                                if (!hasExplicitDiscount)
                                {
                                    var originalDiscountAmount = orderToCheck.DiscountAmount;
                                    orderToCheck.DiscountAmount = 0;
                                    orderToCheck.TotalAmount = orderToCheck.SubTotal + orderToCheck.TaxAmount;
                                    await _context.SaveChangesAsync();
                                    _logger.LogWarning("AUTO-DISCOUNT PREVENTION (UpdateOrder): Reset automatic discount of {DiscountAmount} for order {OrderId}", 
                                        originalDiscountAmount, order.OrderId);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing loyalty points for completed order {OrderId}", order.OrderId);
                        }
                    });
                }
                else if (oldStatus == "completed" && (order.Status == "cancelled" || order.Status == "refunded"))
                {
                    // ÄÆ¡n hÃ ng bá»‹ há»§y hoáº·c hoÃ n tráº£ - hoÃ n Ä‘iá»ƒm
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("Processing loyalty points refund for cancelled/refunded order {OrderId}", order.OrderId);
                            await _loyaltyService.ProcessOrderPointsAsync(order.OrderId, isRefund: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing loyalty points refund for order {OrderId}", order.OrderId);
                        }
                    });
                }
            }
            
            return Ok(new { order.OrderId, Status = "Updated", NewStatus = order.Status, CancellationReason = order.CancellationReason });
        }

        // XÃ³a Ä‘Æ¡n hÃ ng
        [HttpDelete("{id}")]
        public IActionResult DeleteOrder(int id)
        {
            try
            {
                Console.WriteLine($"Attempting to delete order {id}");
                
                // TÃ¬m order trÆ°á»›c
                var order = _context.Orders.Find(id);
                if (order == null) 
                {
                    Console.WriteLine($"Order {id} not found");
                    return NotFound(new { message = $"ÄÆ¡n hÃ ng #{id} khÃ´ng tá»“n táº¡i" });
                }
                
                Console.WriteLine($"Found order {id}, deleting in correct order...");
                
                // BÆ°á»›c 1: XÃ³a Notifications liÃªn quan Ä‘áº¿n order nÃ y trÆ°á»›c
                var notifications = _context.Notifications.Where(n => n.OrderId == id).ToList();
                Console.WriteLine($"Found {notifications.Count} notifications to delete");
                
                if (notifications.Any())
                {
                    _context.Notifications.RemoveRange(notifications);
                    _context.SaveChanges();
                }
                
                // BÆ°á»›c 2: XÃ³a OrderItems
                var orderItems = _context.OrderItems.Where(oi => oi.OrderId == id).ToList();
                Console.WriteLine($"Found {orderItems.Count} order items to delete");
                
                if (orderItems.Any())
                {
                    _context.OrderItems.RemoveRange(orderItems);
                }
                
                // BÆ°á»›c 3: Cuá»‘i cÃ¹ng xÃ³a Order
                _context.Orders.Remove(order);
                _context.SaveChanges();
                
                Console.WriteLine($"Successfully deleted order {id}");
                return Ok(new { Status = "Deleted", OrderId = id, Message = $"ÄÃ£ xÃ³a Ä‘Æ¡n hÃ ng #{id}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    message = "Lá»—i khi xÃ³a Ä‘Æ¡n hÃ ng", 
                    error = ex.Message, 
                    orderId = id 
                });
            }
        }

        // PhÆ°Æ¡ng thá»©c POST JSON Ä‘á»ƒ táº¡o Ä‘Æ¡n hÃ ng
        [HttpPost("json")]
        public async Task<IActionResult> CreateOrderFromJson([FromBody] CreateOrderRequest request)
        {
            try
            {
                if (request?.OrderItems == null || !request.OrderItems.Any())
                {
                    return BadRequest(new { message = "ÄÆ¡n hÃ ng pháº£i cÃ³ Ã­t nháº¥t má»™t sáº£n pháº©m" });
                }

                var insufficientStockProducts = new List<string>();
                var lowStockProducts = new List<string>();

                // Kiá»ƒm tra vÃ  trá»« tá»“n kho cho tá»«ng sáº£n pháº©m
                foreach (var orderItem in request.OrderItems)
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductId);
                    if (product != null)
                    {
                        // Kiá»ƒm tra xem cÃ³ Ä‘á»§ tá»“n kho hay khÃ´ng
                        if (product.StockQuantity < orderItem.Quantity)
                        {
                            insufficientStockProducts.Add($"{product.Name} (cÃ²n {product.StockQuantity}, cáº§n {orderItem.Quantity})");
                            continue;
                        }

                        // Trá»« tá»“n kho
                        product.StockQuantity -= orderItem.Quantity;

                        // Kiá»ƒm tra tá»“n kho tháº¥p sau khi trá»«
                        if (product.StockQuantity <= product.MinStockLevel)
                        {
                            lowStockProducts.Add($"{product.Name} (cÃ²n {product.StockQuantity})");
                        }
                    }
                }

                // Náº¿u cÃ³ sáº£n pháº©m khÃ´ng Ä‘á»§ tá»“n kho, tráº£ vá» lá»—i
                if (insufficientStockProducts.Any())
                {
                    return BadRequest(new 
                    { 
                        message = "KhÃ´ng Ä‘á»§ tá»“n kho cho cÃ¡c sáº£n pháº©m", 
                        products = insufficientStockProducts 
                    });
                }

                // Táº¡o Ä‘Æ¡n hÃ ng
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

                // ThÃªm OrderItems
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

                // Táº¡o thÃ´ng bÃ¡o Ä‘Æ¡n hÃ ng má»›i
                try
                {
                    var notification = new Notification
                    {
                        Type = NotificationType.NewOrder,
                        Title = "ÄÆ¡n hÃ ng má»›i",
                        Message = $"KhÃ¡ch hÃ ng {order.CustomerName ?? "VÃ£ng lai"} vá»«a Ä‘áº·t Ä‘Æ¡n hÃ ng #{order.OrderId}",
                        OrderId = order.OrderId,
                        Metadata = JsonSerializer.Serialize(new
                        {
                            CustomerName = order.CustomerName ?? "VÃ£ng lai",
                            TotalAmount = order.TotalAmount,
                            FormattedTotal = order.TotalAmount.ToString("N0") + "Ä‘",
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

                // Táº¡o thÃ´ng bÃ¡o tá»“n kho tháº¥p náº¿u cÃ³
                if (lowStockProducts.Any())
                {
                    try
                    {
                        var lowStockNotification = new Notification
                        {
                            Type = NotificationType.LowStock,
                            Title = "Cáº£nh bÃ¡o tá»“n kho tháº¥p",
                            Message = $"CÃ¡c sáº£n pháº©m sau cÃ³ tá»“n kho tháº¥p: {string.Join(", ", lowStockProducts)}",
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
                    message = "ÄÆ¡n hÃ ng Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng",
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
                    message = "Lá»—i khi táº¡o Ä‘Æ¡n hÃ ng", 
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

