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

        // GET: api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers([FromQuery] int? storeId = null)
        {
            var query = _context.Customers.AsQueryable();
            
            // Filter by store if storeId is provided
            if (storeId.HasValue)
            {
                query = query.Where(c => c.StoreId == storeId.Value);
            }
            
            return await query.ToListAsync();
        }

        // POST: api/customers
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromForm] string hoTen, [FromForm] string soDienThoai, [FromForm] string email, [FromForm] string diaChi, [FromForm] string hangKhachHang)
        {
            // Chuyển đổi hạng khách hàng
            if (!Enum.TryParse<CustomerRank>(hangKhachHang, true, out var rank))
                return BadRequest("Hạng khách hàng không hợp lệ");

            var customer = new Customer
            {
                HoTen = hoTen,
                SoDienThoai = soDienThoai,
                Email = email,
                DiaChi = diaChi,
                HangKhachHang = rank
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        // PUT: api/customers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromForm] string hoTen, [FromForm] string soDienThoai, [FromForm] string email, [FromForm] string diaChi, [FromForm] string hangKhachHang)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            if (!Enum.TryParse<CustomerRank>(hangKhachHang, true, out var rank))
                return BadRequest("Hạng khách hàng không hợp lệ");
            customer.HoTen = hoTen;
            customer.SoDienThoai = soDienThoai;
            customer.Email = email;
            customer.DiaChi = diaChi;
            customer.HangKhachHang = rank;
            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        // DELETE: api/customers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
