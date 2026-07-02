using Microsoft.EntityFrameworkCore;
using PMS.Data;

namespace PMS.Services
{
    /// <summary>
    /// Aligns dbo.Possession with db.txt / EF model (Scripts/Sql/20260208_Possession_Module.sql).
    /// </summary>
    public static class PossessionSchemaEnsurer
    {
        public static async Task EnsureAsync(PMSDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(CreateTableIfMissingSql);
            await context.Database.ExecuteSqlRawAsync(MergeLegacyPluralTableSql);
            await context.Database.ExecuteSqlRawAsync(WidenPrimaryKeySql);
            await context.Database.ExecuteSqlRawAsync(AddMissingColumnsSql);
            await context.Database.ExecuteSqlRawAsync(BackfillDatesSql);
            await context.Database.ExecuteSqlRawAsync(SeedWorkflowConfigSql);
            await context.Database.ExecuteSqlRawAsync(WidenActivityLogRefIdSql);
        }

        private const string CreateTableIfMissingSql = @"
IF OBJECT_ID(N'dbo.Possession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Possession (
        [PossessionID] NVARCHAR(40) NOT NULL,
        [PropertyID] CHAR(10) NULL,
        [CustomerID] CHAR(10) NULL,
        [PossessionDate] DATETIME NOT NULL CONSTRAINT DF_Possession_PossessionDate DEFAULT (GETDATE()),
        [WorkFlowStatus] NVARCHAR(100) NULL,
        [Comments] NVARCHAR(MAX) NULL,
        [Remarks] NVARCHAR(255) NULL,
        [PossessionDueCharges] DECIMAL(18,2) NOT NULL CONSTRAINT DF_Possession_DueCharges DEFAULT (0),
        [PossessionPaidCharges] DECIMAL(18,2) NOT NULL CONSTRAINT DF_Possession_PaidCharges DEFAULT (0),
        [BankName] NVARCHAR(200) NULL,
        [PaidDate] DATETIME2(0) NULL,
        [InstrumentNo] NVARCHAR(100) NULL,
        [PaymentMethod] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(0) NULL,
        [CreatedBy] CHAR(10) NULL,
        [ModifiedAt] DATETIME2(0) NULL,
        [ModifiedBy] CHAR(10) NULL,
        [ApprovedBy] CHAR(10) NULL,
        [ApprovedAt] DATETIME2(0) NULL,
        [DeclinedBy] CHAR(10) NULL,
        [DeclinedAt] DATETIME2(0) NULL,
        CONSTRAINT [PK_Possession] PRIMARY KEY CLUSTERED ([PossessionID])
    );

    IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'FK_Possession_Customers')
        ALTER TABLE dbo.Possession WITH NOCHECK
            ADD CONSTRAINT [FK_Possession_Customers] FOREIGN KEY ([CustomerID]) REFERENCES [dbo].[Customers] ([CustomerID]);

    IF OBJECT_ID(N'dbo.Property', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'FK_Possession_Property')
        ALTER TABLE dbo.Possession WITH NOCHECK
            ADD CONSTRAINT [FK_Possession_Property] FOREIGN KEY ([PropertyID]) REFERENCES [dbo].[Property] ([PropertyID]);
END;";

        private const string MergeLegacyPluralTableSql = @"
IF OBJECT_ID(N'dbo.Possessions', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Possession', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.Possession (PossessionID, PropertyID, CustomerID, PossessionDate, WorkFlowStatus, Comments, Remarks)
    SELECT
        p.PossessionID,
        p.PropertyID,
        p.CustomerID,
        p.PossessionDate,
        ISNULL(NULLIF(LTRIM(RTRIM(p.[Status])), N''), N'Initiated'),
        NULL,
        p.Remarks
    FROM dbo.Possessions AS p
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Possession AS x WHERE x.PossessionID = p.PossessionID);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Possessions', N'U') AND name = N'FK_Possessions_Customer')
        ALTER TABLE dbo.Possessions DROP CONSTRAINT FK_Possessions_Customer;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Possessions', N'U') AND name = N'FK_Possessions_Property')
        ALTER TABLE dbo.Possessions DROP CONSTRAINT FK_Possessions_Property;

    DROP TABLE dbo.Possessions;
END;";

        private const string WidenPrimaryKeySql = @"
IF EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Possession', N'U')
      AND c.name = N'PossessionID'
      AND t.name IN (N'char', N'nchar')
      AND c.max_length = 10
)
BEGIN
    DECLARE @pkName sysname =
        (SELECT kc.name FROM sys.key_constraints AS kc
         WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Possession', N'U') AND kc.type = N'PK');
    IF @pkName IS NOT NULL
        EXEC(N'ALTER TABLE dbo.Possession DROP CONSTRAINT [' + @pkName + N'];');

    ALTER TABLE dbo.Possession ALTER COLUMN PossessionID NVARCHAR(40) NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Possession', N'U') AND type = N'PK')
        ALTER TABLE dbo.Possession ADD CONSTRAINT PK_Possession PRIMARY KEY CLUSTERED (PossessionID);
END;";

        private const string AddMissingColumnsSql = @"
IF OBJECT_ID(N'dbo.Possession', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'PossessionDueCharges')
        ALTER TABLE dbo.Possession ADD PossessionDueCharges DECIMAL(18,2) NOT NULL CONSTRAINT DF_Possession_Due DEFAULT (0);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'PossessionPaidCharges')
        ALTER TABLE dbo.Possession ADD PossessionPaidCharges DECIMAL(18,2) NOT NULL CONSTRAINT DF_Possession_Paid DEFAULT (0);
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'BankName')
        ALTER TABLE dbo.Possession ADD BankName NVARCHAR(200) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'PaidDate')
        ALTER TABLE dbo.Possession ADD PaidDate DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'InstrumentNo')
        ALTER TABLE dbo.Possession ADD InstrumentNo NVARCHAR(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'PaymentMethod')
        ALTER TABLE dbo.Possession ADD PaymentMethod NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'CreatedAt')
        ALTER TABLE dbo.Possession ADD CreatedAt DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'CreatedBy')
        ALTER TABLE dbo.Possession ADD CreatedBy CHAR(10) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'ModifiedAt')
        ALTER TABLE dbo.Possession ADD ModifiedAt DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'ModifiedBy')
        ALTER TABLE dbo.Possession ADD ModifiedBy CHAR(10) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'ApprovedBy')
        ALTER TABLE dbo.Possession ADD ApprovedBy CHAR(10) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'ApprovedAt')
        ALTER TABLE dbo.Possession ADD ApprovedAt DATETIME2(0) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'DeclinedBy')
        ALTER TABLE dbo.Possession ADD DeclinedBy CHAR(10) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Possession', N'U') AND name = N'DeclinedAt')
        ALTER TABLE dbo.Possession ADD DeclinedAt DATETIME2(0) NULL;
END;";

        private const string BackfillDatesSql = @"
IF OBJECT_ID(N'dbo.Possession', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Possession', N'PossessionDate') IS NOT NULL
        UPDATE dbo.Possession SET PossessionDate = COALESCE(PossessionDate, SYSUTCDATETIME()) WHERE PossessionDate IS NULL;
    IF COL_LENGTH(N'dbo.Possession', N'CreatedAt') IS NOT NULL
        UPDATE dbo.Possession SET CreatedAt = COALESCE(CreatedAt, PossessionDate, SYSUTCDATETIME()) WHERE CreatedAt IS NULL;
END;";

        private const string SeedWorkflowConfigSql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Configuration WHERE ConfigKey = N'possessionworkflow')
BEGIN
    INSERT INTO dbo.Configuration (ConfigKey, Category, ConfigValue, Description, CreatedAt)
    VALUES (
        N'possessionworkflow',
        N'Possession',
        N'Initiated,Operations Desk,Approved,Declined',
        N'Possession workflow (comma-separated). Move between Initiated and Operations Desk; Approve/Decline from Operations Desk.',
        GETDATE()
    );
END;";

        private const string WidenActivityLogRefIdSql = @"
IF OBJECT_ID(N'dbo.ActivityLog', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ActivityLog', N'RefID') IS NOT NULL
   AND COL_LENGTH(N'dbo.ActivityLog', N'RefID') < 100
BEGIN
    ALTER TABLE dbo.ActivityLog ALTER COLUMN RefID NVARCHAR(100) NULL;
END;";
    }
}
