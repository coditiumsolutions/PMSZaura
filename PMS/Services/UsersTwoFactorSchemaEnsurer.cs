using Microsoft.EntityFrameworkCore;
using PMS.Data;

namespace PMS.Services
{
    /// <summary>
    /// Aligns dbo.Users with db.txt / EF model (Scripts/Sql/20260213_Users_TwoFactor_Enforce2FA.sql).
    /// </summary>
    public static class UsersTwoFactorSchemaEnsurer
    {
        public static async Task EnsureAsync(PMSDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(AddColumnsSql);
            await context.Database.ExecuteSqlRawAsync(SeedEnforce2FASql);
        }

        private const string AddColumnsSql = @"
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Users', 'TwoFactorEnabled') IS NULL
        ALTER TABLE dbo.Users ADD TwoFactorEnabled bit NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT (0);

    IF COL_LENGTH('dbo.Users', 'TwoFactorSecret') IS NULL
        ALTER TABLE dbo.Users ADD TwoFactorSecret nvarchar(500) NULL;
END;";

        private const string SeedEnforce2FASql = @"
IF OBJECT_ID(N'dbo.Configuration', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Configuration WHERE ConfigKey = N'Enforce2FA')
BEGIN
    INSERT INTO dbo.Configuration (ConfigKey, Category, ConfigValue, Description, CreatedAt)
    VALUES (
        N'Enforce2FA',
        N'Security',
        N'false',
        N'When true, all users must complete Google Authenticator (TOTP) setup or verification after password. When false, only users with TwoFactorEnabled are prompted.',
        GETDATE()
    );
END;";
    }
}
