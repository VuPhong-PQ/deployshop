using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to map CustomerRank to Vietnamese tier names
        private string MapCustomerRankToFrontend(CustomerRank rank)
        {
            return rank switch
            {
                CustomerRank.Thuong => "Đồng",     // TierId 1
                CustomerRank.Premium => "Bạc",     // TierId 2  
                CustomerRank.VIP => "Vàng",        // TierId 3 - ĐÚNG cho khách hàng hiện tại
                CustomerRank.Platinum => "Kim cương", // TierId 4
                _ => "Đồng"
            };
        }

        // Helper method to map frontend names to CustomerRank (hỗ trợ cả tiếng Anh và Việt)
        private CustomerRank MapFrontendToCustomerRank(string frontendRank)
        {
            return frontendRank?.ToLower() switch
            {
                "bronze" or "đồng" => CustomerRank.Thuong,
                "silver" or "bạc" => CustomerRank.Premium,
                "gold" or "vàng" => CustomerRank.VIP,
                "platinum" or "kim cương" or "kim cuong" => CustomerRank.Platinum,
                _ => CustomerRank.Thuong
            };
        }
        public class CreateCustomerDto
        {
            public string? HoTen { get; set; }
            public string? SoDienThoai { get; set; }
            public string? Email { get; set; }
            public string? DiaChi { get; set; }
            public string? HangKhachHang { get; set; }
            public int? StoreId { get; set; }
        }

                // GET: api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomers([FromQuery] int? storeId = null)
        {
            var query = _context.Customers
                .Where(c => c.IsActive) // Only show active customers
                .Include(c => c.CustomerTier) // Include tier information
                .AsQueryable();
            
            // Don't filter by store - show all customers regardless of StoreId
            // This allows customers to be shared across stores
            
            var customers = await query.ToListAsync();
            
            // Map to frontend-friendly format
            var result = customers.Select(c => new
            {
                customerId = c.CustomerId,
                hoTen = c.HoTen,
                soDienThoai = c.SoDienThoai,
                email = c.Email,
                diaChi = c.DiaChi,
                hangKhachHang = MapCustomerRankToFrontend(c.HangKhachHang),
                tierId = c.TierId,
                customerTier = c.CustomerTier != null ? new
                {
                    tierId = c.CustomerTier.TierId,
                    tierName = c.CustomerTier.TierName,
                    discountPercentage = c.CustomerTier.DiscountPercentage,
                    pointsMultiplier = c.CustomerTier.PointsMultiplier,
                    tierColor = c.CustomerTier.TierColor
                } : null,
                storeId = c.StoreId,
                loyaltyPoints = c.LoyaltyPoints,
                totalSpent = c.TotalSpent,
                dateOfBirth = c.DateOfBirth,
                isActive = c.IsActive,
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt
            });
            
            return Ok(result);
        }        // GET: api/customers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Where(c => c.IsActive) // Only show active customers
                .Include(c => c.CustomerTier)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(c => c.LoyaltyTransactions)
                .FirstOrDefaultAsync(c => c.CustomerId == id);
                
            if (customer == null) return NotFound("Customer not found or has been deactivated");
            
            var result = new
            {
                customerId = customer.CustomerId,
                hoTen = customer.HoTen,
                soDienThoai = customer.SoDienThoai,
                email = customer.Email,
                diaChi = customer.DiaChi,
                hangKhachHang = MapCustomerRankToFrontend(customer.HangKhachHang),
                storeId = customer.StoreId,
                loyaltyPoints = customer.LoyaltyPoints,
                totalSpent = customer.TotalSpent,
                tierId = customer.TierId,
                dateOfBirth = customer.DateOfBirth,
                isActive = customer.IsActive,
                createdAt = customer.CreatedAt,
                updatedAt = customer.UpdatedAt,
                orders = customer.Orders?.Select(o => new
                {
                    orderId = o.OrderId,
                    orderNumber = o.OrderNumber,
                    customerId = o.CustomerId,
                    storeId = o.StoreId,
                    totalAmount = o.TotalAmount.ToString("F2"),
                    subTotal = o.SubTotal.ToString("F2"),
                    taxAmount = o.TaxAmount.ToString("F2"),
                    discountAmount = o.DiscountAmount.ToString("F2"),
                    status = o.Status,
                    paymentMethod = o.PaymentMethod,
                    createdAt = o.CreatedAt,
                    items = o.Items?.Select(i => new
                    {
                        orderItemId = i.OrderItemId,
                        orderId = i.OrderId,
                        productId = i.ProductId,
                        quantity = i.Quantity,
                        price = i.Price.ToString("F2"),
                        totalPrice = i.TotalPrice.ToString("F2"),
                        productName = i.ProductName,
                        product = i.Product != null ? new
                        {
                            productId = i.Product.ProductId,
                            name = i.Product.Name,
                            barcode = i.Product.Barcode,
                            price = i.Product.Price.ToString("F2")
                        } : null
                    }).ToList()
                }).ToList(),
                loyaltyTransactions = customer.LoyaltyTransactions?.Select(t => new
                {
                    transactionId = t.TransactionId,
                    customerId = t.CustomerId,
                    orderId = t.OrderId,
                    transactionType = t.TransactionType,
                    points = t.Points,
                    pointsBalance = t.PointsBalance,
                    reason = t.Reason,
                    expiryDate = t.ExpiryDate,
                    processedAt = t.ProcessedAt
                }).ToList()
            };
            
            return Ok(result);
        }

        // POST: api/customers
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto, [FromQuery] int? storeId = null)
        {
            try
            {
                // Map frontend rank to backend enum
                var rank = MapFrontendToCustomerRank(dto.HangKhachHang ?? "Bronze");

                var customer = new Customer
                {
                    HoTen = dto.HoTen,
                    SoDienThoai = dto.SoDienThoai,
                    Email = dto.Email,
                    DiaChi = dto.DiaChi,
                    HangKhachHang = rank,
                    StoreId = storeId ?? dto.StoreId // Use storeId from query parameter or dto
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                
                // Return in frontend-friendly format
                var result = new
                {
                    customerId = customer.CustomerId,
                    hoTen = customer.HoTen,
                    soDienThoai = customer.SoDienThoai,
                    email = customer.Email,
                    diaChi = customer.DiaChi,
                    hangKhachHang = customer.HangKhachHang.ToString(),
                    storeId = customer.StoreId
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi tạo khách hàng: {ex.Message}");
            }
        }        // PUT: api/customers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null) return NotFound();

                // Parse customer rank using mapping function
                var rank = MapFrontendToCustomerRank(dto.HangKhachHang);

                customer.HoTen = dto.HoTen;
                customer.SoDienThoai = dto.SoDienThoai;
                customer.Email = dto.Email;
                customer.DiaChi = dto.DiaChi;
                customer.HangKhachHang = rank;
                if (dto.StoreId.HasValue)
                {
                    customer.StoreId = dto.StoreId;
                }

                await _context.SaveChangesAsync();
                
                // Return in frontend-friendly format
                var result = new
                {
                    customerId = customer.CustomerId,
                    hoTen = customer.HoTen,
                    soDienThoai = customer.SoDienThoai,
                    email = customer.Email,
                    diaChi = customer.DiaChi,
                    hangKhachHang = MapCustomerRankToFrontend(customer.HangKhachHang),
                    storeId = customer.StoreId
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi cập nhật khách hàng: {ex.Message}");
            }
        }

        // DELETE: api/customers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null) return NotFound();
                
                // Soft delete: Mark customer as inactive instead of hard delete
                customer.IsActive = false;
                customer.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Customer deactivated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi xóa khách hàng: {ex.Message}");
            }
        }

        // GET: api/customers/orders/{orderId}
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound($"Không tìm thấy đơn hàng với ID: {orderId}");
                }

                var result = new
                {
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    customerId = order.CustomerId,
                    storeId = order.StoreId,
                    status = order.Status.ToString().ToLower(),
                    totalAmount = order.TotalAmount,
                    subTotal = order.SubTotal,
                    taxAmount = order.TaxAmount,
                    discountAmount = order.DiscountAmount,
                    paymentMethod = order.PaymentMethod?.ToString().ToLower(),
                    createdAt = order.CreatedAt,
                    items = order.Items.Select(item => new
                    {
                        orderItemId = item.OrderItemId,
                        orderId = item.OrderId,
                        productId = item.ProductId,
                        quantity = item.Quantity,
                        price = item.Price,
                        totalPrice = item.TotalPrice,
                        productName = item.Product?.Name,
                        product = item.Product != null ? new
                        {
                            productId = item.Product.ProductId,
                            name = item.Product.Name,
                            sku = item.Product.Barcode,
                            price = item.Product.Price,
                            description = item.Product.Description
                        } : null
                    }).ToList(),
                    customer = order.Customer != null ? new
                    {
                        customerId = order.Customer.CustomerId,
                        hoTen = order.Customer.HoTen,
                        soDienThoai = order.Customer.SoDienThoai
                    } : null
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi lấy chi tiết đơn hàng: {ex.Message}");
            }
        }

        [HttpGet("inactive")]
        public async Task<ActionResult<IEnumerable<object>>> GetInactiveCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Where(c => !c.IsActive) // Only inactive customers
                    .Include(c => c.CustomerTier)
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving inactive customers", error = ex.Message });
            }
        }

        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                customer.IsActive = true;
                customer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Customer restored successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error restoring customer", error = ex.Message });
            }
        }
    }
}
