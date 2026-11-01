using Microsoft.EntityFrameworkCore;
using Salzee.Data;

namespace Salzee.Services;

/// <summary>AMS access rules: hard delete is allowed only for <see cref="Models.User.UserType"/> = Admin.</summary>
public class AmsAccessService
{
    private readonly PMSDbContext _context;

    public AmsAccessService(PMSDbContext context) => _context = context;

    public async Task<bool> IsAdminUserAsync(string? userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var userType = await _context.Users.AsNoTracking()
            .Where(u => u.UserID == userId)
            .Select(u => u.UserType)
            .FirstOrDefaultAsync(ct);

        return string.Equals(userType?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
