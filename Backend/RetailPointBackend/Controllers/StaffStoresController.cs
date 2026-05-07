using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StaffStoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffStoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StaffStores/{staffId}/stores
        [HttpGet("{staffId}/stores")]
        public async Task<ActionResult<IEnumerable<Store>>> GetStaffStores(int staffId)
        {
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.StaffId == staffId);

            if (staff == null)
            {
                return NotFound("Staff not found");
            }

            // Admin cÃ³ thá»ƒ truy cáº­p táº¥t cáº£ stores
            if (staff.Role.RoleName == "Admin")
            {
                return await _context.Stores
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }

            // NhÃ¢n viÃªn thÆ°á»ng chá»‰ Ä‘Æ°á»£c truy cáº­p stores Ä‘Æ°á»£c phÃ¢n quyá»n
            var assignedStores = await _context.StaffStores
                .Where(ss => ss.StaffId == staffId)
                .Include(ss => ss.Store)
                .Where(ss => ss.Store.IsActive)
                .Select(ss => ss.Store)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return assignedStores;
        }

        // GET: api/StaffStores/my-stores - Láº¥y stores mÃ  user hiá»‡n táº¡i cÃ³ quyá»n truy cáº­p
        [HttpGet("my-stores")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyStores()
        {
            // Láº¥y thÃ´ng tin user tá»« token (giáº£ sá»­ cÃ³ middleware xá»­ lÃ½)
            // Táº¡m thá»i láº¥y tá»« header hoáº·c session, sau nÃ y cÃ³ thá»ƒ láº¥y tá»« JWT token
            var username = HttpContext.Request.Headers["Username"].FirstOrDefault() ?? "admin";
            
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
                        manager = s.Manager,
                        notes = s.Notes
                    })
                    .OrderBy(s => s.name)
                    .ToListAsync();
                return Ok(allStores);
            }

            // Láº¥y stores Ä‘Æ°á»£c assign cho staff nÃ y
            var assignedStores = await _context.StaffStores
                .Where(ss => ss.StaffId == staff.StaffId)
                .Include(ss => ss.Store)
                .Where(ss => ss.Store.IsActive)
                .Select(ss => new { 
                    storeId = ss.Store.StoreId,
                    name = ss.Store.Name,
                    address = ss.Store.Address,
                    phone = ss.Store.Phone,
                    manager = ss.Store.Manager,
                    notes = ss.Store.Notes
                })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Ok(assignedStores);
        }

        // POST: api/StaffStores/set-current-store - Äáº·t cá»­a hÃ ng hiá»‡n táº¡i cho session
        [HttpPost("set-current-store")]
        public async Task<IActionResult> SetCurrentStore([FromBody] SetCurrentStoreDto request)
        {
            var username = HttpContext.Request.Headers["Username"].FirstOrDefault() ?? "admin";
            
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                
            if (staff == null)
            {
                return Unauthorized("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin nhÃ¢n viÃªn");
            }

            // Kiá»ƒm tra xem store cÃ³ tá»“n táº¡i vÃ  active khÃ´ng
            var store = await _context.Stores.FindAsync(request.StoreId);
            if (store == null || !store.IsActive)
            {
                return BadRequest("Cá»­a hÃ ng khÃ´ng tá»“n táº¡i hoáº·c khÃ´ng hoáº¡t Ä‘á»™ng");
            }

            // Kiá»ƒm tra quyá»n truy cáº­p store
            var hasAccess = false;
            if (staff.Role.RoleName == "Admin")
            {
                hasAccess = true;
            }
            else
            {
                hasAccess = await _context.StaffStores
                    .AnyAsync(ss => ss.StaffId == staff.StaffId && ss.StoreId == request.StoreId);
            }

            if (!hasAccess)
            {
                return StatusCode(403, "Báº¡n khÃ´ng cÃ³ quyá»n truy cáº­p cá»­a hÃ ng nÃ y");
            }

            // LÆ°u store hiá»‡n táº¡i vÃ o session hoáº·c return Ä‘á»ƒ frontend lÆ°u
            HttpContext.Session.SetInt32("CurrentStoreId", request.StoreId);
            
            return Ok(new { 
                message = "ÄÃ£ chuyá»ƒn Ä‘á»•i cá»­a hÃ ng thÃ nh cÃ´ng",
                storeId = request.StoreId,
                storeName = store.Name
            });
        }

        // GET: api/StaffStores/current-store - Láº¥y thÃ´ng tin cá»­a hÃ ng hiá»‡n táº¡i
        [HttpGet("current-store")]
        public async Task<ActionResult<object>> GetCurrentStore()
        {
            var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
            
            if (currentStoreId == null)
            {
                // Náº¿u chÆ°a set, láº¥y store Ä‘áº§u tiÃªn mÃ  user cÃ³ quyá»n
                var myStoresResult = await GetMyStores();
                if (myStoresResult.Result is OkObjectResult okResult)
                {
                    var stores = okResult.Value as IEnumerable<object>;
                    var firstStore = stores?.FirstOrDefault();
                    if (firstStore != null)
                    {
                        var storeData = firstStore.GetType().GetProperty("storeId")?.GetValue(firstStore);
                        if (storeData != null)
                        {
                            currentStoreId = (int)storeData;
                            HttpContext.Session.SetInt32("CurrentStoreId", currentStoreId.Value);
                        }
                    }
                }
            }

            if (currentStoreId == null)
            {
                return Ok(new { message = "ChÆ°a cÃ³ cá»­a hÃ ng nÃ o Ä‘Æ°á»£c chá»n" });
            }

            var store = await _context.Stores
                .Where(s => s.StoreId == currentStoreId && s.IsActive)
                .Select(s => new {
                    storeId = s.StoreId,
                    name = s.Name,
                    address = s.Address,
                    phone = s.Phone,
                    manager = s.Manager,
                    notes = s.Notes
                })
                .FirstOrDefaultAsync();

            if (store == null)
            {
                return NotFound("Cá»­a hÃ ng hiá»‡n táº¡i khÃ´ng tá»“n táº¡i");
            }

            return Ok(store);
        }

        // GET: api/StaffStores/{staffId}/available-stores
        [HttpGet("{staffId}/available-stores")]
        public async Task<ActionResult<IEnumerable<Store>>> GetAvailableStores(int staffId)
        {
            // Láº¥y táº¥t cáº£ stores Ä‘ang hoáº¡t Ä‘á»™ng
            var allStores = await _context.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Láº¥y stores Ä‘Ã£ Ä‘Æ°á»£c assign cho staff nÃ y
            var assignedStoreIds = await _context.StaffStores
                .Where(ss => ss.StaffId == staffId)
                .Select(ss => ss.StoreId)
                .ToListAsync();

            // Tráº£ vá» stores chÆ°a Ä‘Æ°á»£c assign
            var availableStores = allStores
                .Where(s => !assignedStoreIds.Contains(s.StoreId))
                .ToList();

            return availableStores;
        }

        // POST: api/StaffStores/assign
        [HttpPost("assign")]
        public async Task<IActionResult> AssignStoreToStaff(AssignStoreDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiá»ƒm tra staff tá»“n táº¡i
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.StaffId == request.StaffId);

            if (staff == null)
            {
                return NotFound("Staff not found");
            }

            // Admin khÃ´ng cáº§n assign stores (cÃ³ quyá»n truy cáº­p táº¥t cáº£)
            if (staff.Role.RoleName == "Admin")
            {
                return BadRequest("Admin khÃ´ng cáº§n phÃ¢n quyá»n cá»­a hÃ ng");
            }

            // Kiá»ƒm tra store tá»“n táº¡i vÃ  Ä‘ang hoáº¡t Ä‘á»™ng
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == request.StoreId && s.IsActive);

            if (store == null)
            {
                return NotFound("Store not found or inactive");
            }

            // Kiá»ƒm tra Ä‘Ã£ Ä‘Æ°á»£c assign chÆ°a
            var existingAssignment = await _context.StaffStores
                .FirstOrDefaultAsync(ss => ss.StaffId == request.StaffId && ss.StoreId == request.StoreId);

            if (existingAssignment != null)
            {
                return BadRequest("Staff Ä‘Ã£ Ä‘Æ°á»£c phÃ¢n quyá»n vÃ o cá»­a hÃ ng nÃ y");
            }

            // Táº¡o assignment má»›i
            var staffStore = new StaffStore
            {
                StaffId = request.StaffId,
                StoreId = request.StoreId
            };

            _context.StaffStores.Add(staffStore);
            await _context.SaveChangesAsync();

            return Ok(new { message = "PhÃ¢n quyá»n cá»­a hÃ ng thÃ nh cÃ´ng" });
        }

        // DELETE: api/StaffStores/unassign
        [HttpDelete("unassign")]
        public async Task<IActionResult> UnassignStoreFromStaff(int staffId, int storeId)
        {
            var staffStore = await _context.StaffStores
                .FirstOrDefaultAsync(ss => ss.StaffId == staffId && ss.StoreId == storeId);

            if (staffStore == null)
            {
                return NotFound("Assignment not found");
            }

            _context.StaffStores.Remove(staffStore);
            await _context.SaveChangesAsync();

            return Ok(new { message = "ÄÃ£ há»§y phÃ¢n quyá»n cá»­a hÃ ng" });
        }

        // GET: api/StaffStores/staff-assignments
        [HttpGet("staff-assignments")]
        public async Task<ActionResult> GetStaffAssignments()
        {
            var staffAssignments = await _context.Staffs
                .Include(s => s.Role)
                .Include(s => s.StaffStores)
                    .ThenInclude(ss => ss.Store)
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.StaffId,
                    s.FullName,
                    s.Username,
                    RoleName = s.Role.RoleName,
                    IsAdmin = s.Role.RoleName == "Admin",
                    AssignedStores = s.Role.RoleName == "Admin" 
                        ? new List<object>() // Admin khÃ´ng cáº§n hiá»ƒn thá»‹ stores cá»¥ thá»ƒ
                        : s.StaffStores.Where(ss => ss.Store.IsActive)
                            .Select(ss => new
                            {
                                StoreId = ss.Store.StoreId,
                                Name = ss.Store.Name,
                                Address = ss.Store.Address
                            }).Cast<object>().ToList()
                })
                .ToListAsync();

            return Ok(staffAssignments);
        }
    }

    // DTO classes
    public class AssignStoreDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required]
        public int StoreId { get; set; }
    }


}
