using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PMS.Data;

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
                await UsersTwoFactorSchemaEnsurer.EnsureAsync(context);
                await EnsureAccSchemaAsync(context);

                // Seed initial data
                await seedService.SeedAsync();
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                Console.WriteLine($"Database initialization error: {ex}");
                throw;
            }
        }

        private static async Task EnsureAccSchemaAsync(PMSDbContext context)
        {
            // The EF model expects acc.ARReceipt / acc.ARReceiptAllocation.
            // If they are missing, integration flows will fail with:
            // "Invalid object name 'acc.ARReceipt'".
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State != System.Data.ConnectionState.Open;
            if (shouldClose)
            {
                await conn.OpenAsync();
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(N'acc.ARReceipt', N'U') IS NULL THEN 1 ELSE 0 END";
            var missing = Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
            if (!missing)
            {
                if (shouldClose)
                {
                    await conn.CloseAsync();
                }
                return;
            }

            // Try to locate and execute the idempotent schema creation script.
            // We remove `GO` batch separators so the script can be executed in one command.
            var scriptPath = ResolveScriptPath("Scripts", "AMS_Create_acc_schema.sql") ??
                              Path.Combine(AppContext.BaseDirectory, "Scripts", "AMS_Create_acc_schema.sql");

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException(
                    $"Cannot create acc schema because '{scriptPath}' was not found. Run Scripts/AMS_Create_acc_schema.sql against the target database once.");
            }

            var raw = await File.ReadAllTextAsync(scriptPath);
            var cleaned = string.Join(Environment.NewLine,
                raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                   .Where(line =>
                   {
                       var t = line.Trim();
                       return !string.Equals(t, "GO", StringComparison.OrdinalIgnoreCase)
                              && !string.Equals(t, "GO;", StringComparison.OrdinalIgnoreCase);
                   })
            );

            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
            await context.Database.ExecuteSqlRawAsync(cleaned);

            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }

        private static string? ResolveScriptPath(string relativeFolder, string fileName)
        {
            // Walk up from the current runtime directory to find the repository folder.
            // This makes local dev work even if the working directory changes.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 10 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, relativeFolder, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            return null;
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
