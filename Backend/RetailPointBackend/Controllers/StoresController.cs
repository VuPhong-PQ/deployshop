using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Stores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Store>>> GetStores()
        {
            return await _context.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // GET: api/Stores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Store>> GetStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);

            if (store == null)
            {
                return NotFound();
            }

            return store;
        }

        // POST: api/Stores
        [HttpPost]
        public async Task<ActionResult<Store>> CreateStore(Store store)
        {
            store.CreatedAt = DateTime.Now;
            store.UpdatedAt = DateTime.Now;
            
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStore), new { id = store.StoreId }, store);
        }

        // PUT: api/Stores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStore(int id, Store store)
        {
            if (id != store.StoreId)
            {
                return BadRequest();
            }

            var existingStore = await _context.Stores.FindAsync(id);
            if (existingStore == null)
            {
                return NotFound();
            }

            existingStore.Name = store.Name;
            existingStore.Address = store.Address;
            existingStore.Phone = store.Phone;
            existingStore.Email = store.Email;
            existingStore.TaxCode = store.TaxCode;
            existingStore.Manager = store.Manager;
            existingStore.IsActive = store.IsActive;
            existingStore.Notes = store.Notes;
            existingStore.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StoreExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Stores/5 (Soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
            {
                return NotFound();
            }

            // Check if store has staff assigned
            var hasStaff = await _context.Staffs.AnyAsync(s => s.StoreId == id && s.IsActive);
            if (hasStaff)
            {
                return BadRequest("Không thể xóa cửa hàng có nhân viên đang hoạt động");
            }

            store.IsActive = false;
            store.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Stores/{id}/staff
        [HttpGet("{id}/staff")]
        public async Task<ActionResult<IEnumerable<Staff>>> GetStoreStaff(int id)
        {
            var storeExists = await _context.Stores.AnyAsync(s => s.StoreId == id && s.IsActive);
            if (!storeExists)
            {
                return NotFound("Store not found");
            }

            var staff = await _context.Staffs
                .Include(s => s.Role)
                .Where(s => s.StoreId == id && s.IsActive)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return staff;
        }

        // GET: api/Stores/all (For admin - includes inactive stores)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Store>>> GetAllStores()
        {
            return await _context.Stores
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // POST: api/Stores/{id}/activate
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
            {
                return NotFound();
            }

            store.IsActive = true;
            store.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cửa hàng đã được kích hoạt" });
        }

        // POST: api/Stores/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
            {
                return NotFound();
            }

            // Check if store has active staff
            var hasActiveStaff = await _context.Staffs.AnyAsync(s => s.StoreId == id && s.IsActive);
            if (hasActiveStaff)
            {
                return BadRequest("Không thể vô hiệu hóa cửa hàng có nhân viên đang hoạt động");
            }

            store.IsActive = false;
            store.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cửa hàng đã được vô hiệu hóa" });
        }

        // GET: api/Stores/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetStoreStats()
        {
            var totalStores = await _context.Stores.CountAsync();
            var activeStores = await _context.Stores.CountAsync(s => s.IsActive);
            var inactiveStores = totalStores - activeStores;

            var storeStats = await _context.Stores
                .Select(s => new
                {
                    s.StoreId,
                    s.Name,
                    s.IsActive,
                    StaffCount = _context.Staffs.Count(st => st.StoreId == s.StoreId && st.IsActive),
                    ProductCount = _context.Products.Count(p => p.StoreId == s.StoreId),
                    OrderCount = _context.Orders.Count(o => o.StoreId == s.StoreId.ToString())
                })
                .ToListAsync();

            return Ok(new
            {
                summary = new
                {
                    totalStores,
                    activeStores,
                    inactiveStores
                },
                stores = storeStats
            });
        }

        private bool StoreExists(int id)
        {
            return _context.Stores.Any(e => e.StoreId == id);
        }
    }

    // DTO classes for better API design
    public class CreateStoreDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? TaxCode { get; set; }

        [StringLength(200)]
        public string? Manager { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateStoreDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? TaxCode { get; set; }

        [StringLength(200)]
        public string? Manager { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}