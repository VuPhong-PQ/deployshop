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

        // Helper method to map CustomerRank to frontend-friendly names
        private string MapCustomerRankToFrontend(CustomerRank rank)
        {
            return rank switch
            {
                CustomerRank.Thuong => "Bronze",
                CustomerRank.Premium => "Silver", 
                CustomerRank.VIP => "Gold",
                CustomerRank.Platinum => "Platinum",
                _ => "Bronze"
            };
        }

        // Helper method to map frontend names to CustomerRank
        private CustomerRank MapFrontendToCustomerRank(string frontendRank)
        {
            return frontendRank?.ToLower() switch
            {
                "bronze" => CustomerRank.Thuong,
                "silver" => CustomerRank.Premium,
                "gold" => CustomerRank.VIP,
                "platinum" => CustomerRank.Platinum,
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
            var query = _context.Customers.AsQueryable();
            
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
                storeId = c.StoreId
            });
            
            return Ok(result);
        }        // GET: api/customers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            
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
                
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi xóa khách hàng: {ex.Message}");
            }
        }
    }
}
