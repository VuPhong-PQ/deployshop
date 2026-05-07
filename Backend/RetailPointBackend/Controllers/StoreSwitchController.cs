using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StoreSwitchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreSwitchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StoreSwitch/my-stores - Láº¥y stores mÃ  user hiá»n táº¡i cÃ³ quyá»n truy cáº­p
        [HttpGet("my-stores")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyStores()
        {
            // Láº¥y username tá»« JWT claim
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username))
                return Unauthorized("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin xÃ¡c thá»±c");
            
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                
            if (staff == null)
            {
                return Unauthorized("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin nhÃ¢n viÃªn");
            }

            // Náº¿u lÃ  Admin thÃ¬ cÃ³ quyá»n truy cáº­p táº¥t cáº£ stores
            if (staff.Role.RoleName == "Admin")
            {
                var allStores = await _context.Stores
                    .Where(s => s.IsActive)
                    .Select(s => new { 
                        storeId = s.StoreId,
                        name = s.Name,
                        address = s.Address,
                        phone = s.Phone,
                        manager = s.Manager
                    })
                    .OrderBy(s => s.name)
                    .ToListAsync();
                return Ok(allStores);
            }

            // Láº¥y stores ÄÆ°á»£c assign cho staff nÃ y
            var assignedStores = await _context.StaffStores
                .Where(ss => ss.StaffId == staff.StaffId)
                .Include(ss => ss.Store)
                .Where(ss => ss.Store.IsActive)
                .Select(ss => new { 
                    storeId = ss.Store.StoreId,
                    name = ss.Store.Name,
                    address = ss.Store.Address,
                    phone = ss.Store.Phone,
                    manager = ss.Store.Manager
                })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Ok(assignedStores);
        }

        // POST: api/StoreSwitch/set-current - Set store hiá»n táº¡i cho session
        [HttpPost("set-current")]
        public async Task<IActionResult> SetCurrentStore([FromBody] SetCurrentStoreDto request)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username))
                return Unauthorized("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin xÃ¡c thá»±c");
            
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                
            if (staff == null)
            {
                return Unauthorized("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin nhÃ¢n viÃªn");
            }

            // Kiá»m tra xem store cÃ³ tá»n táº¡i vÃ  Äang hoáº¡t Äá»ng khÃ´ng
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == request.StoreId && s.IsActive);
                
            if (store == null)
            {
                return NotFound("Cá»­a hÃ ng khÃ´ng tá»n táº¡i hoáº·c ÄÃ£ bá» vÃ´ hiá»u hÃ³a");
            }

            // Kiá»m tra quyá»n truy cáº­p
            if (staff.Role.RoleName != "Admin")
            {
                var hasAccess = await _context.StaffStores
                    .AnyAsync(ss => ss.StaffId == staff.StaffId && ss.StoreId == request.StoreId);
                    
                if (!hasAccess)
                {
                    return StatusCode(403, "Báº¡n khÃ´ng cÃ³ quyá»n truy cáº­p cá»­a hÃ ng nÃ y");
                }
            }

            // LÆ°u vÃ o session
            HttpContext.Session.SetInt32("CurrentStoreId", request.StoreId);
            HttpContext.Session.SetString("CurrentStoreName", store.Name);

            return Ok(new { 
                message = "ÄÃ£ chuyá»n Äá»i cá»­a hÃ ng thÃ nh cÃ´ng",
                storeId = store.StoreId,
                storeName = store.Name
            });
        }

        // GET: api/StoreSwitch/current - Láº¥y thÃ´ng tin store hiá»n táº¡i
        [HttpGet("current")]
        public async Task<ActionResult<object>> GetCurrentStore()
        {
            var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
            
            if (currentStoreId == null)
            {
                return Ok(new { message = "ChÆ°a cÃ³ cá»­a hÃ ng nÃ o ÄÆ°á»£c chá»n", storeId = (int?)null, storeName = (string?)null });
            }

            // Kiá»m tra store váº«n cÃ²n hoáº¡t Äá»ng
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == currentStoreId && s.IsActive);
                
            if (store == null)
            {
                // Clear session náº¿u store khÃ´ng cÃ²n hoáº¡t Äá»ng
                HttpContext.Session.Remove("CurrentStoreId");
                HttpContext.Session.Remove("CurrentStoreName");
                return Ok(new { message = "Cá»­a hÃ ng ÄÃ£ bá» vÃ´ hiá»u hÃ³a", storeId = (int?)null, storeName = (string?)null });
            }

            return Ok(new {
                storeId = currentStoreId,
                storeName = store.Name,
                address = store.Address,
                manager = store.Manager
            });
        }

        // GET: api/StoreSwitch/current-info - Láº¥y thÃ´ng tin ngáº¯n gá»n cho header
        [HttpGet("current-info")]
        public async Task<ActionResult<object>> GetCurrentStoreInfo()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username))
                return Unauthorized();
            var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
            
            // Náº¿u chÆ°a cÃ³ store ÄÆ°á»£c set, láº¥y store Äáº§u tiÃªn mÃ  user cÃ³ quyá»n
            if (currentStoreId == null)
            {
                var staff = await _context.Staffs
                    .Include(s => s.Role)
                    .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                    
                if (staff != null)
                {
                    Store? firstStore = null;
                    
                    if (staff.Role.RoleName == "Admin")
                    {
                        firstStore = await _context.Stores
                            .Where(s => s.IsActive)
                            .OrderBy(s => s.Name)
                            .FirstOrDefaultAsync();
                    }
                    else
                    {
                        var staffStore = await _context.StaffStores
                            .Include(ss => ss.Store)
                            .Where(ss => ss.StaffId == staff.StaffId && ss.Store.IsActive)
                            .OrderBy(ss => ss.Store.Name)
                            .FirstOrDefaultAsync();
                        firstStore = staffStore?.Store;
                    }

                    if (firstStore != null)
                    {
                        currentStoreId = firstStore.StoreId;
                        HttpContext.Session.SetInt32("CurrentStoreId", currentStoreId.Value);
                        HttpContext.Session.SetString("CurrentStoreName", firstStore.Name);
                        
                        return Ok(new {
                            storeId = firstStore.StoreId,
                            storeName = firstStore.Name,
                            shortName = firstStore.Name.Length > 20 ? firstStore.Name.Substring(0, 20) + "..." : firstStore.Name
                        });
                    }
                }
                
                return Ok(new { message = "ChÆ°a cÃ³ cá»­a hÃ ng ÄÆ°á»£c chá»n" });
            }

            // Láº¥y thÃ´ng tin store hiá»n táº¡i
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == currentStoreId && s.IsActive);
                
            if (store == null)
            {
                HttpContext.Session.Remove("CurrentStoreId");
                HttpContext.Session.Remove("CurrentStoreName");
                return Ok(new { message = "Cá»­a hÃ ng khÃ´ng hoáº¡t Äá»ng" });
            }

            return Ok(new {
                storeId = store.StoreId,
                storeName = store.Name,
                shortName = store.Name.Length > 20 ? store.Name.Substring(0, 20) + "..." : store.Name
            });
        }
    }

    public class SetCurrentStoreDto
    {
        [Required]
        public int StoreId { get; set; }
    }
}