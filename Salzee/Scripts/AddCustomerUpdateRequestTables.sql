IF OBJECT_ID('dbo.CustomerUpdateRequestChanges', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.CustomerUpdateRequestChanges;
END
GO

IF OBJECT_ID('dbo.CustomerUpdateRequests', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.CustomerUpdateRequests;
END
GO

CREATE TABLE dbo.CustomerUpdateRequests (
    RequestID CHAR(10) NOT NULL PRIMARY KEY,
    CustomerID CHAR(10) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_CustomerUpdateRequests_Status DEFAULT ('Pending'),
    ProposedDataJson NVARCHAR(MAX) NULL,
    OriginalDataJson NVARCHAR(MAX) NULL,
    ReviewerComments NVARCHAR(MAX) NULL,
    RequestedBy CHAR(10) NULL,
    RequestedAt DATETIME NOT NULL CONSTRAINT DF_CustomerUpdateRequests_RequestedAt DEFAULT (GETDATE()),
    ApprovedBy CHAR(10) NULL,
    ApprovedAt DATETIME NULL,
    RejectedBy CHAR(10) NULL,
    RejectedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_CustomerUpdateRequests_CreatedAt DEFAULT (GETDATE())
);
GO

ALTER TABLE dbo.CustomerUpdateRequests
ADD CONSTRAINT FK_CustomerUpdateRequests_Customers
    FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID) ON DELETE CASCADE;
GO

ALTER TABLE dbo.CustomerUpdateRequests
ADD CONSTRAINT FK_CustomerUpdateRequests_RequestedBy
    FOREIGN KEY (RequestedBy) REFERENCES dbo.Users(UserID) ON DELETE NO ACTION;
GO

ALTER TABLE dbo.CustomerUpdateRequests
ADD CONSTRAINT FK_CustomerUpdateRequests_ApprovedBy
    FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserID) ON DELETE NO ACTION;
GO

ALTER TABLE dbo.CustomerUpdateRequests
ADD CONSTRAINT FK_CustomerUpdateRequests_RejectedBy
    FOREIGN KEY (RejectedBy) REFERENCES dbo.Users(UserID) ON DELETE NO ACTION;
GO

CREATE TABLE dbo.CustomerUpdateRequestChanges (
    Id CHAR(10) NOT NULL PRIMARY KEY,
    RequestID CHAR(10) NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_CustomerUpdateRequestChanges_CreatedAt DEFAULT (GETDATE())
);
GO

ALTER TABLE dbo.CustomerUpdateRequestChanges
ADD CONSTRAINT FK_CustomerUpdateRequestChanges_Requests
    FOREIGN KEY (RequestID) REFERENCES dbo.CustomerUpdateRequests(RequestID) ON DELETE CASCADE;
GO

CREATE INDEX IX_CustomerUpdateRequests_Customer_Status
ON dbo.CustomerUpdateRequests(CustomerID, Status, RequestedAt DESC);
GO

CREATE INDEX IX_CustomerUpdateRequestChanges_RequestID
ON dbo.CustomerUpdateRequestChanges(RequestID);
GO
