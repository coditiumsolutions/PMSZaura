-- Two-factor authentication (TOTP / Google Authenticator) + global toggle
-- Run against your PMS database (e.g. SSMS or sqlcmd).

IF COL_LENGTH('dbo.Users', 'TwoFactorEnabled') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD TwoFactorEnabled bit NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Users', 'TwoFactorSecret') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD TwoFactorSecret nvarchar(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configuration WHERE ConfigKey = N'Enforce2FA')
BEGIN
    INSERT INTO dbo.Configuration (ConfigKey, Category, ConfigValue, Description, CreatedAt)
    VALUES (
        N'Enforce2FA',
        N'Security',
        N'false',
        N'When true, all users must complete Google Authenticator (TOTP) setup or verification after password. When false, only users with TwoFactorEnabled are prompted.',
        GETDATE()
    );
END
GO
