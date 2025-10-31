using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
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

            // Admin có thể truy cập tất cả stores
            if (staff.Role.RoleName == "Admin")
            {
                return await _context.Stores
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }

            // Nhân viên thường chỉ được truy cập stores được phân quyền
            var assignedStores = await _context.StaffStores
                .Where(ss => ss.StaffId == staffId)
                .Include(ss => ss.Store)
                .Where(ss => ss.Store.IsActive)
                .Select(ss => ss.Store)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return assignedStores;
        }

        // GET: api/StaffStores/my-stores - Lấy stores mà user hiện tại có quyền truy cập
        [HttpGet("my-stores")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyStores()
        {
            // Lấy thông tin user từ token (giả sử có middleware xử lý)
            // Tạm thời lấy từ header hoặc session, sau này có thể lấy từ JWT token
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
                        manager = s.Manager,
                        notes = s.Notes
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
                    manager = ss.Store.Manager,
                    notes = ss.Store.Notes
                })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Ok(assignedStores);
        }

        // POST: api/StaffStores/set-current-store - Đặt cửa hàng hiện tại cho session
        [HttpPost("set-current-store")]
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

            // Kiểm tra xem store có tồn tại và active không
            var store = await _context.Stores.FindAsync(request.StoreId);
            if (store == null || !store.IsActive)
            {
                return BadRequest("Cửa hàng không tồn tại hoặc không hoạt động");
            }

            // Kiểm tra quyền truy cập store
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
                return StatusCode(403, "Bạn không có quyền truy cập cửa hàng này");
            }

            // Lưu store hiện tại vào session hoặc return để frontend lưu
            HttpContext.Session.SetInt32("CurrentStoreId", request.StoreId);
            
            return Ok(new { 
                message = "Đã chuyển đổi cửa hàng thành công",
                storeId = request.StoreId,
                storeName = store.Name
            });
        }

        // GET: api/StaffStores/current-store - Lấy thông tin cửa hàng hiện tại
        [HttpGet("current-store")]
        public async Task<ActionResult<object>> GetCurrentStore()
        {
            var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
            
            if (currentStoreId == null)
            {
                // Nếu chưa set, lấy store đầu tiên mà user có quyền
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
                return Ok(new { message = "Chưa có cửa hàng nào được chọn" });
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
                return NotFound("Cửa hàng hiện tại không tồn tại");
            }

            return Ok(store);
        }

        // GET: api/StaffStores/{staffId}/available-stores
        [HttpGet("{staffId}/available-stores")]
        public async Task<ActionResult<IEnumerable<Store>>> GetAvailableStores(int staffId)
        {
            // Lấy tất cả stores đang hoạt động
            var allStores = await _context.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Lấy stores đã được assign cho staff này
            var assignedStoreIds = await _context.StaffStores
                .Where(ss => ss.StaffId == staffId)
                .Select(ss => ss.StoreId)
                .ToListAsync();

            // Trả về stores chưa được assign
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

            // Kiểm tra staff tồn tại
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.StaffId == request.StaffId);

            if (staff == null)
            {
                return NotFound("Staff not found");
            }

            // Admin không cần assign stores (có quyền truy cập tất cả)
            if (staff.Role.RoleName == "Admin")
            {
                return BadRequest("Admin không cần phân quyền cửa hàng");
            }

            // Kiểm tra store tồn tại và đang hoạt động
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.StoreId == request.StoreId && s.IsActive);

            if (store == null)
            {
                return NotFound("Store not found or inactive");
            }

            // Kiểm tra đã được assign chưa
            var existingAssignment = await _context.StaffStores
                .FirstOrDefaultAsync(ss => ss.StaffId == request.StaffId && ss.StoreId == request.StoreId);

            if (existingAssignment != null)
            {
                return BadRequest("Staff đã được phân quyền vào cửa hàng này");
            }

            // Tạo assignment mới
            var staffStore = new StaffStore
            {
                StaffId = request.StaffId,
                StoreId = request.StoreId
            };

            _context.StaffStores.Add(staffStore);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Phân quyền cửa hàng thành công" });
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

            return Ok(new { message = "Đã hủy phân quyền cửa hàng" });
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
                        ? new List<object>() // Admin không cần hiển thị stores cụ thể
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