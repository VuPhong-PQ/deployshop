using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Staff
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffResponseDto>>> GetStaffs()
        {
            var staffs = await _context.Staffs
                .Include(s => s.Role)
                .Select(s => new StaffResponseDto
                {
                    StaffId = s.StaffId,
                    FullName = s.FullName,
                    Username = s.Username,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    RoleId = s.RoleId,
                    RoleName = s.Role.RoleName,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    LastLogin = s.LastLogin,
                    Notes = s.Notes
                })
                .ToListAsync();

            return Ok(staffs);
        }

        // GET: api/Staff/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StaffResponseDto>> GetStaff(int id)
        {
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .Where(s => s.StaffId == id)
                .Select(s => new StaffResponseDto
                {
                    StaffId = s.StaffId,
                    FullName = s.FullName,
                    Username = s.Username,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    RoleId = s.RoleId,
                    RoleName = s.Role.RoleName,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    LastLogin = s.LastLogin,
                    Notes = s.Notes
                })
                .FirstOrDefaultAsync();

            if (staff == null)
            {
                return NotFound();
            }

            return Ok(staff);
        }

        // POST: api/Staff
        [HttpPost]
        public async Task<ActionResult<StaffResponseDto>> CreateStaff(CreateStaffDto createStaffDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if username already exists
            if (await _context.Staffs.AnyAsync(s => s.Username == createStaffDto.Username))
            {
                return BadRequest("Username đã tồn tại");
            }

            // Check if role exists
            var role = await _context.Roles.FindAsync(createStaffDto.RoleId);
            if (role == null)
            {
                return BadRequest("Role không tồn tại");
            }

            var staff = new Staff
            {
                FullName = createStaffDto.FullName,
                Username = createStaffDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createStaffDto.Password),
                Email = createStaffDto.Email,
                PhoneNumber = createStaffDto.PhoneNumber,
                RoleId = createStaffDto.RoleId,
                IsActive = createStaffDto.IsActive ?? true,
                Notes = createStaffDto.Notes
            };

            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();

            var responseDto = new StaffResponseDto
            {
                StaffId = staff.StaffId,
                FullName = staff.FullName,
                Username = staff.Username,
                Email = staff.Email,
                PhoneNumber = staff.PhoneNumber,
                RoleId = staff.RoleId,
                RoleName = role.RoleName,
                IsActive = staff.IsActive,
                CreatedAt = staff.CreatedAt,
                LastLogin = staff.LastLogin,
                Notes = staff.Notes
            };

            return CreatedAtAction(nameof(GetStaff), new { id = staff.StaffId }, responseDto);
        }

        // PUT: api/Staff/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, UpdateStaffDto updateStaffDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            // Check if new username already exists (excluding current staff)
            if (!string.IsNullOrEmpty(updateStaffDto.Username) && 
                updateStaffDto.Username != staff.Username &&
                await _context.Staffs.AnyAsync(s => s.Username == updateStaffDto.Username && s.StaffId != id))
            {
                return BadRequest("Username đã tồn tại");
            }

            // Check if role exists
            if (updateStaffDto.RoleId.HasValue)
            {
                var role = await _context.Roles.FindAsync(updateStaffDto.RoleId.Value);
                if (role == null)
                {
                    return BadRequest("Role không tồn tại");
                }
                staff.RoleId = updateStaffDto.RoleId.Value;
            }

            // Update fields
            if (!string.IsNullOrEmpty(updateStaffDto.FullName))
                staff.FullName = updateStaffDto.FullName;
            
            if (!string.IsNullOrEmpty(updateStaffDto.Username))
                staff.Username = updateStaffDto.Username;
            
            if (!string.IsNullOrEmpty(updateStaffDto.Password))
                staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateStaffDto.Password);
            
            if (!string.IsNullOrEmpty(updateStaffDto.Email))
                staff.Email = updateStaffDto.Email;
            
            if (!string.IsNullOrEmpty(updateStaffDto.PhoneNumber))
                staff.PhoneNumber = updateStaffDto.PhoneNumber;
            
            if (updateStaffDto.IsActive.HasValue)
                staff.IsActive = updateStaffDto.IsActive.Value;
            
            if (!string.IsNullOrEmpty(updateStaffDto.Notes))
                staff.Notes = updateStaffDto.Notes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StaffExists(id))
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

        // DELETE: api/Staff/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            // Check if staff has any related records
            var hasOrders = await _context.Orders.AnyAsync(o => o.StaffId == id);
            var hasInventoryTransactions = await _context.InventoryTransactions.AnyAsync(it => it.StaffId == id);

            if (hasOrders || hasInventoryTransactions)
            {
                // Soft delete if staff has related records to maintain data integrity
                staff.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Nhân viên đã được đánh dấu không hoạt động do có dữ liệu liên quan", softDelete = true });
            }
            else
            {
                // Hard delete if no related records exist
                _context.Staffs.Remove(staff);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        // POST: api/Staff/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var staff = await _context.Staffs
                .Include(s => s.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.Username == loginDto.Username && s.IsActive);

            if (staff == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, staff.PasswordHash))
            {
                return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng");
            }

            // Update last login
            staff.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            var permissions = staff.Role.RolePermissions
                .Select(rp => rp.Permission.PermissionName)
                .ToList();

            var response = new LoginResponseDto
            {
                StaffId = staff.StaffId,
                FullName = staff.FullName,
                Username = staff.Username,
                Email = staff.Email,
                RoleId = staff.RoleId,
                RoleName = staff.Role.RoleName,
                Permissions = permissions,
                LastLogin = staff.LastLogin,
                StoreId = staff.StoreId,
                StoreName = staff.Store?.Name
            };

            return Ok(response);
        }

        // GET: api/Staff/refresh-permissions/{staffId}
        [HttpGet("refresh-permissions/{staffId}")]
        public async Task<ActionResult<object>> RefreshPermissions(int staffId)
        {
            var staff = await _context.Staffs
                .Include(s => s.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(s => s.StaffId == staffId && s.IsActive);

            if (staff == null)
            {
                return NotFound("Nhân viên không tồn tại hoặc không hoạt động");
            }

            var permissions = staff.Role.RolePermissions
                .Select(rp => rp.Permission.PermissionName)
                .ToList();

            return Ok(new
            {
                staffId = staff.StaffId,
                fullName = staff.FullName,
                username = staff.Username,
                email = staff.Email,
                roleId = staff.RoleId,
                roleName = staff.Role.RoleName,
                permissions = permissions,
                lastLogin = staff.LastLogin
            });
        }

        private bool StaffExists(int id)
        {
            return _context.Staffs.Any(e => e.StaffId == id);
        }
    }

    // DTOs
    public class StaffResponseDto
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateStaffDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public int RoleId { get; set; }

        public bool? IsActive { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateStaffDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(50)]
        public string? Username { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public string? Notes { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
        public DateTime? LastLogin { get; set; }
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
    }
}