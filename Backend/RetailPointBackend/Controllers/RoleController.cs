using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Role
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Select(r => new RoleResponseDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        PermissionId = rp.Permission.PermissionId,
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description,
                        Category = rp.Permission.Category
                    }).ToList()
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/Role/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleResponseDto>> GetRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(r => r.RoleId == id)
                .Select(r => new RoleResponseDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt,
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        PermissionId = rp.Permission.PermissionId,
                        PermissionName = rp.Permission.PermissionName,
                        Description = rp.Permission.Description,
                        Category = rp.Permission.Category
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return NotFound();
            }

            return Ok(role);
        }

        // POST: api/Role
        [HttpPost]
        public async Task<ActionResult<RoleResponseDto>> CreateRole(CreateRoleDto createRoleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if role name already exists
            if (await _context.Roles.AnyAsync(r => r.RoleName == createRoleDto.RoleName))
            {
                return BadRequest("Tên role đã tồn tại");
            }

            var role = new Role
            {
                RoleName = createRoleDto.RoleName,
                Description = createRoleDto.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add permissions if provided
            if (createRoleDto.PermissionIds != null && createRoleDto.PermissionIds.Any())
            {
                foreach (var permissionId in createRoleDto.PermissionIds)
                {
                    var permission = await _context.Permissions.FindAsync(permissionId);
                    if (permission != null)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.RoleId,
                            PermissionId = permissionId
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            var responseDto = new RoleResponseDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                Permissions = new List<PermissionDto>()
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.RoleId }, responseDto);
        }

        // PUT: api/Role/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto updateRoleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Check if new role name already exists (excluding current role)
            if (!string.IsNullOrEmpty(updateRoleDto.RoleName) && 
                updateRoleDto.RoleName != role.RoleName &&
                await _context.Roles.AnyAsync(r => r.RoleName == updateRoleDto.RoleName && r.RoleId != id))
            {
                return BadRequest("Tên role đã tồn tại");
            }

            // Update fields
            if (!string.IsNullOrEmpty(updateRoleDto.RoleName))
                role.RoleName = updateRoleDto.RoleName;
            
            if (!string.IsNullOrEmpty(updateRoleDto.Description))
                role.Description = updateRoleDto.Description;

            // Update permissions if provided
            if (updateRoleDto.PermissionIds != null)
            {
                // Remove existing permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();
                _context.RolePermissions.RemoveRange(existingPermissions);

                // Add new permissions
                foreach (var permissionId in updateRoleDto.PermissionIds)
                {
                    var permission = await _context.Permissions.FindAsync(permissionId);
                    if (permission != null)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = id,
                            PermissionId = permissionId
                        });
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(id))
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

        // DELETE: api/Role/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Check if role is being used by any staff
            if (await _context.Staffs.AnyAsync(s => s.RoleId == id))
            {
                return BadRequest("Không thể xóa role đang được sử dụng bởi nhân viên");
            }

            // Remove role permissions first
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToListAsync();
            _context.RolePermissions.RemoveRange(rolePermissions);

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Role/assign-permission
        [HttpPost("assign-permission")]
        public async Task<ActionResult> AssignPermissionToRole([FromBody] AssignPermissionDto dto)
        {
            // Kiểm tra role và permission có tồn tại không
            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
            if (!roleExists)
            {
                return NotFound("Role not found");
            }

            var permissionExists = await _context.Permissions.AnyAsync(p => p.PermissionId == dto.PermissionId);
            if (!permissionExists)
            {
                return NotFound("Permission not found");
            }

            // Kiểm tra xem permission đã được assign chưa
            var existingAssignment = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

            if (existingAssignment != null)
            {
                return BadRequest("Permission already assigned to this role");
            }

            // Tạo assignment mới
            var rolePermission = new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId,
                CreatedAt = DateTime.Now
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Permission assigned successfully" });
        }

        // POST: api/Role/remove-permission
        [HttpPost("remove-permission")]
        public async Task<ActionResult> RemovePermissionFromRole([FromBody] AssignPermissionDto dto)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

            if (rolePermission == null)
            {
                return NotFound("Permission assignment not found");
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Permission removed successfully" });
        }

        private bool RoleExists(int id)
        {
            return _context.Roles.Any(e => e.RoleId == id);
        }
    }

    // DTOs
    public class RoleResponseDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }

    public class PermissionDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
    }

    public class CreateRoleDto
    {
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public List<int>? PermissionIds { get; set; }
    }

    public class UpdateRoleDto
    {
        [StringLength(50)]
        public string? RoleName { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public List<int>? PermissionIds { get; set; }
    }

    public class AssignPermissionDto
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }
}