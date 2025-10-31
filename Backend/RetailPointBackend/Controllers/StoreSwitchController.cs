using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreSwitchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreSwitchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StoreSwitch/my-stores - Lấy stores mà user hiện tại có quyền truy cập
        [HttpGet("my-stores")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyStores()
        {
            // Lấy thông tin user từ token (tạm thời từ header)
            var username = HttpContext.Request.Headers["Username"].FirstOrDefault() ?? "admin";
            
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                
            if (staff == null)
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            // Nếu là Admin thì có quyền truy cập tất cả stores
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

            // Lấy stores được assign cho staff này
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

        // POST: api/StoreSwitch/set-current - Set store hiện tại cho session
        [HttpPost("set-current")]
        public async Task<IActionResult> SetCurrentStore([FromBody] SetCurrentStoreDto request)
        {
            var username = HttpContext.Request.Headers["Username"].FirstOrDefault() ?? "admin";
            
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                
            if (staff == null)
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            // Kiểm tra xem store có tồn tại và đang hoạt động không
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == request.StoreId && s.IsActive);
                
            if (store == null)
            {
                return NotFound("Cửa hàng không tồn tại hoặc đã bị vô hiệu hóa");
            }

            // Kiểm tra quyền truy cập
            if (staff.Role.RoleName != "Admin")
            {
                var hasAccess = await _context.StaffStores
                    .AnyAsync(ss => ss.StaffId == staff.StaffId && ss.StoreId == request.StoreId);
                    
                if (!hasAccess)
                {
                    return StatusCode(403, "Bạn không có quyền truy cập cửa hàng này");
                }
            }

            // Lưu vào session
            HttpContext.Session.SetInt32("CurrentStoreId", request.StoreId);
            HttpContext.Session.SetString("CurrentStoreName", store.Name);

            return Ok(new { 
                message = "Đã chuyển đổi cửa hàng thành công",
                storeId = store.StoreId,
                storeName = store.Name
            });
        }

        // GET: api/StoreSwitch/current - Lấy thông tin store hiện tại
        [HttpGet("current")]
        public async Task<ActionResult<object>> GetCurrentStore()
        {
            var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
            
            if (currentStoreId == null)
            {
                return Ok(new { message = "Chưa có cửa hàng nào được chọn", storeId = (int?)null, storeName = (string?)null });
            }

            // Kiểm tra store vẫn còn hoạt động
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == currentStoreId && s.IsActive);
                
            if (store == null)
            {
                // Clear session nếu store không còn hoạt động
                HttpContext.Session.Remove("CurrentStoreId");
                HttpContext.Session.Remove("CurrentStoreName");
                return Ok(new { message = "Cửa hàng đã bị vô hiệu hóa", storeId = (int?)null, storeName = (string?)null });
            }

            return Ok(new {
                storeId = currentStoreId,
                storeName = store.Name,
                address = store.Address,
                manager = store.Manager
            });
        }

        // GET: api/StoreSwitch/current-info - Lấy thông tin ngắn gọn cho header
        [HttpGet("current-info")]
        public async Task<ActionResult<object>> GetCurrentStoreInfo()
        {
            var username = HttpContext.Request.Headers["Username"].FirstOrDefault() ?? "admin";
            var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
            
            // Nếu chưa có store được set, lấy store đầu tiên mà user có quyền
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
                
                return Ok(new { message = "Chưa có cửa hàng được chọn" });
            }

            // Lấy thông tin store hiện tại
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == currentStoreId && s.IsActive);
                
            if (store == null)
            {
                HttpContext.Session.Remove("CurrentStoreId");
                HttpContext.Session.Remove("CurrentStoreName");
                return Ok(new { message = "Cửa hàng không hoạt động" });
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