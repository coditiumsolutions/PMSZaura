using Microsoft.EntityFrameworkCore;
using Salzee.Data;

namespace Salzee.Services
{
    public interface ITwoFactorConfigService
    {
        /// <summary>When true, every user must pass TOTP setup or verification after password.</summary>
        Task<bool> IsEnforce2FAAsync(CancellationToken cancellationToken = default);
    }

    public class TwoFactorConfigService : ITwoFactorConfigService
    {
        private const string ConfigKey = "Enforce2FA";
        private readonly PMSDbContext _context;

        public TwoFactorConfigService(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEnforce2FAAsync(CancellationToken cancellationToken = default)
        {
            var row = await _context.Configurations.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigKey == ConfigKey, cancellationToken);
            if (row?.ConfigValue == null) return false;
            return bool.TryParse(row.ConfigValue.Trim(), out var b) && b;
        }
    }
}
