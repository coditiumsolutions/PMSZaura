using Microsoft.EntityFrameworkCore;
using Salzee.Data;

namespace Salzee.Services
{
    public class ModulePermissionService : IModulePermissionService
    {
        private readonly PMSDbContext _context;

        public ModulePermissionService(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetPermissionAsync(string? userId, string moduleKey)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(moduleKey))
                return "NoAccess";
            var row = await _context.UserModulePermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserID == userId && p.ModuleKey == moduleKey);
            return row?.Permission ?? "NoAccess";
        }

        public bool CanRead(string permission) =>
            permission == "Read" || permission == "Edit" || permission == "Admin";

        public bool CanEdit(string permission) =>
            permission == "Edit" || permission == "Admin";

        public bool CanDelete(string permission) =>
            permission == "Admin";
    }
}
