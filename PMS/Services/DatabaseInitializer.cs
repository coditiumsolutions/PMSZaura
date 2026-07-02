using PMS.Data;
using Microsoft.EntityFrameworkCore;
using PMS.Services;

namespace PMS.Services
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PMSDbContext>();
            var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();

            try
            {
                // Apply pending migrations and create database if it doesn't exist
                context.Database.Migrate();
                await EnsureClientCertificateSecurityTablesAsync(context);
                await PossessionSchemaEnsurer.EnsureAsync(context);

                // Seed initial data
                await seedService.SeedAsync();
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                Console.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        private static async Task EnsureClientCertificateSecurityTablesAsync(PMSDbContext context)
        {
            var sql = @"
IF OBJECT_ID(N'[dbo].[UserMacWhitelist]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserMacWhitelist](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserID] CHAR(10) NOT NULL,
        [MacAddress] NVARCHAR(128) NOT NULL,
        [DeviceName] NVARCHAR(150) NULL,
        [AddedBy] CHAR(10) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_UserMacWhitelist_IsActive] DEFAULT(1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_UserMacWhitelist_CreatedAt] DEFAULT(SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX [UX_UserMacWhitelist_User_Mac] ON [dbo].[UserMacWhitelist]([UserID], [MacAddress]);
    ALTER TABLE [dbo].[UserMacWhitelist] WITH NOCHECK ADD CONSTRAINT [FK_UserMacWhitelist_Users]
        FOREIGN KEY([UserID]) REFERENCES [dbo].[Users]([UserID]) ON DELETE CASCADE;
END;

IF OBJECT_ID(N'[dbo].[UserMacWhitelist]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[UserMacWhitelist]')
          AND name = 'MacAddress'
          AND max_length < 128
    )
    BEGIN
        DROP INDEX [UX_UserMacWhitelist_User_Mac] ON [dbo].[UserMacWhitelist];
        ALTER TABLE [dbo].[UserMacWhitelist] ALTER COLUMN [MacAddress] NVARCHAR(128) NOT NULL;
        CREATE UNIQUE INDEX [UX_UserMacWhitelist_User_Mac] ON [dbo].[UserMacWhitelist]([UserID], [MacAddress]);
    END
END;

IF OBJECT_ID(N'[dbo].[BlockedMacLoginAttempt]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BlockedMacLoginAttempt](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserID] CHAR(10) NOT NULL,
        [MacAddress] NVARCHAR(128) NOT NULL,
        [DeviceName] NVARCHAR(150) NULL,
        [IPAddress] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [IsWhitelisted] BIT NOT NULL CONSTRAINT [DF_BlockedMacLoginAttempt_IsWhitelisted] DEFAULT(0),
        [AttemptedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BlockedMacLoginAttempt_AttemptedAt] DEFAULT(SYSUTCDATETIME()),
        [WhitelistedBy] CHAR(10) NULL,
        [WhitelistedAt] DATETIME2 NULL
    );
    CREATE INDEX [IX_BlockedMacLoginAttempt_User_AttemptedAt] ON [dbo].[BlockedMacLoginAttempt]([UserID], [AttemptedAt] DESC);
    ALTER TABLE [dbo].[BlockedMacLoginAttempt] WITH NOCHECK ADD CONSTRAINT [FK_BlockedMacLoginAttempt_Users]
        FOREIGN KEY([UserID]) REFERENCES [dbo].[Users]([UserID]) ON DELETE CASCADE;
END;";

            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
