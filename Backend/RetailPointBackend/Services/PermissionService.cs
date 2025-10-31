using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int staffId, string permissionName);
        Task<List<string>> GetStaffPermissionsAsync(int staffId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int staffId, string permissionName)
        {
            var staff = await _context.Staffs
                .Include(s => s.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(s => s.StaffId == staffId);

            if (staff == null || !staff.IsActive)
                return false;

            // Check role permissions
            var rolePermissions = staff.Role?.RolePermissions
                .Select(rp => rp.Permission.PermissionName)
                .ToList() ?? new List<string>();

            return rolePermissions.Contains(permissionName);
        }

        public async Task<List<string>> GetStaffPermissionsAsync(int staffId)
        {
            var staff = await _context.Staffs
                .Include(s => s.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(s => s.StaffId == staffId);

            if (staff == null || !staff.IsActive)
                return new List<string>();

            var permissions = new HashSet<string>();

            // Add role permissions
            if (staff.Role?.RolePermissions != null)
            {
                foreach (var rp in staff.Role.RolePermissions)
                {
                    permissions.Add(rp.Permission.PermissionName);
                }
            }

            return permissions.ToList();
        }
    }
}