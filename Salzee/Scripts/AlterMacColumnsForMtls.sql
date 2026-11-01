IF OBJECT_ID(N'[dbo].[UserMacWhitelist]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserMacWhitelist]') AND name = 'MacAddress' AND max_length < 128)
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_UserMacWhitelist_User_Mac' AND object_id = OBJECT_ID(N'[dbo].[UserMacWhitelist]'))
            DROP INDEX [UX_UserMacWhitelist_User_Mac] ON [dbo].[UserMacWhitelist];
        ALTER TABLE [dbo].[UserMacWhitelist] ALTER COLUMN [MacAddress] NVARCHAR(128) NOT NULL;
        CREATE UNIQUE INDEX [UX_UserMacWhitelist_User_Mac] ON [dbo].[UserMacWhitelist]([UserID],[MacAddress]);
    END
END
GO

IF OBJECT_ID(N'[dbo].[BlockedMacLoginAttempt]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BlockedMacLoginAttempt]') AND name = 'MacAddress' AND max_length < 128)
        ALTER TABLE [dbo].[BlockedMacLoginAttempt] ALTER COLUMN [MacAddress] NVARCHAR(128) NOT NULL;
END
GO
