using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Permission
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionResponseDto>>> GetPermissions()
        {
            var permissions = await _context.Permissions
                .Select(p => new PermissionResponseDto
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description,
                    Category = p.Category,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(permissions);
        }

        // GET: api/Permission/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PermissionResponseDto>> GetPermission(int id)
        {
            var permission = await _context.Permissions
                .Where(p => p.PermissionId == id)
                .Select(p => new PermissionResponseDto
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description,
                    Category = p.Category,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (permission == null)
            {
                return NotFound();
            }

            return Ok(permission);
        }

        // POST: api/Permission
        [HttpPost]
        public async Task<ActionResult<PermissionResponseDto>> CreatePermission(CreatePermissionDto createPermissionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if permission name already exists
            if (await _context.Permissions.AnyAsync(p => p.PermissionName == createPermissionDto.PermissionName))
            {
                return BadRequest("Tên permission đã tồn tại");
            }

            var permission = new Permission
            {
                PermissionName = createPermissionDto.PermissionName,
                Description = createPermissionDto.Description,
                Category = createPermissionDto.Category
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            var responseDto = new PermissionResponseDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description,
                Category = permission.Category,
                CreatedAt = permission.CreatedAt
            };

            return CreatedAtAction(nameof(GetPermission), new { id = permission.PermissionId }, responseDto);
        }

        // PUT: api/Permission/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, UpdatePermissionDto updatePermissionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            // Check if new permission name already exists (excluding current permission)
            if (!string.IsNullOrEmpty(updatePermissionDto.PermissionName) && 
                updatePermissionDto.PermissionName != permission.PermissionName &&
                await _context.Permissions.AnyAsync(p => p.PermissionName == updatePermissionDto.PermissionName && p.PermissionId != id))
            {
                return BadRequest("Tên permission đã tồn tại");
            }

            // Update fields
            if (!string.IsNullOrEmpty(updatePermissionDto.PermissionName))
                permission.PermissionName = updatePermissionDto.PermissionName;
            
            if (!string.IsNullOrEmpty(updatePermissionDto.Description))
                permission.Description = updatePermissionDto.Description;
            
            if (!string.IsNullOrEmpty(updatePermissionDto.Category))
                permission.Category = updatePermissionDto.Category;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PermissionExists(id))
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

        // DELETE: api/Permission/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            // Check if permission is being used by any role
            if (await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == id))
            {
                return BadRequest("Không thể xóa permission đang được sử dụng bởi role");
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PermissionExists(int id)
        {
            return _context.Permissions.Any(e => e.PermissionId == id);
        }
    }

    // DTOs
    public class PermissionResponseDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePermissionDto
    {
        [Required]
        [StringLength(50)]
        public string PermissionName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }
    }

    public class UpdatePermissionDto
    {
        [StringLength(50)]
        public string? PermissionName { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }
    }
}