-- ================================================================
-- PMS migration SQL (tables + data)
-- Generated: 2026-05-08 11:21:50
-- Source: localhost / PMS
-- ================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'PMS') IS NULL
BEGIN
    CREATE DATABASE [PMS];
END
GO
USE [PMS];
GO

-- ===================== TABLES ====================

/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 5/8/2026 11:22:09 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProductVersion] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]


GO

/****** Object:  Table [dbo].[ACL]    Script Date: 5/8/2026 11:22:10 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ACL](
	[RoleID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RoleName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Permissions] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[RoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF

GO

/****** Object:  Table [dbo].[ActivityLog]    Script Date: 5/8/2026 11:22:10 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ActivityLog](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Action] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RefType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RefID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Details] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[ActivityLog] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[ActivityLog]  WITH NOCHECK ADD FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL

GO

/****** Object:  Table [dbo].[Allotment]    Script Date: 5/8/2026 11:22:11 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Allotment](
	[AllotmentID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AllottedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AllotmentDate] [datetime] NULL,
	[ApprovedBy] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AllottmentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WorkFlowStatus] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Comments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AdditionalInfo] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[AllotmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Allotment] ADD  DEFAULT (getdate()) FOR [AllotmentDate]
ALTER TABLE [dbo].[Allotment]  WITH NOCHECK ADD FOREIGN KEY([AllottedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Allotment]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Allotment]  WITH NOCHECK ADD FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])

GO

/****** Object:  Table [dbo].[Allotments]    Script Date: 5/8/2026 11:22:11 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Allotments](
	[AllotmentID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AllottedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AllotmentDate] [datetime] NOT NULL,
	[AllottmentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WorkFlowStatus] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ApprovedBy] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AllotmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Allotments] ADD  DEFAULT (getdate()) FOR [AllotmentDate]
ALTER TABLE [dbo].[Allotments] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Allotments]  WITH NOCHECK ADD FOREIGN KEY([AllottedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Allotments]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[Allotments]  WITH NOCHECK ADD FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])
ON DELETE CASCADE

GO

/****** Object:  Table [dbo].[Approvals]    Script Date: 5/8/2026 11:22:11 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Approvals](
	[ApprovalID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RefType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RefID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestedAt] [datetime] NULL,
	[ApprovedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApprovedAt] [datetime] NULL,
	[RejectedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RejectedAt] [datetime] NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[ApprovalID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Approvals]  WITH NOCHECK ADD  CONSTRAINT [FK_Approvals_ApprovedBy] FOREIGN KEY([ApprovedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Approvals] NOCHECK CONSTRAINT [FK_Approvals_ApprovedBy]
ALTER TABLE [dbo].[Approvals]  WITH NOCHECK ADD  CONSTRAINT [FK_Approvals_RejectedBy] FOREIGN KEY([RejectedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Approvals] NOCHECK CONSTRAINT [FK_Approvals_RejectedBy]
ALTER TABLE [dbo].[Approvals]  WITH NOCHECK ADD  CONSTRAINT [FK_Approvals_RequestedBy] FOREIGN KEY([RequestedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Approvals] NOCHECK CONSTRAINT [FK_Approvals_RequestedBy]

GO

/****** Object:  Table [dbo].[Attachments]    Script Date: 5/8/2026 11:22:12 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Attachments](
	[AttachmentID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RefType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RefID] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FileName] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FilePath] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FileType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FileSize] [bigint] NULL,
	[UploadedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[UploadedAt] [datetime] NOT NULL,
	[Description] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AttachmentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[AttachmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Attachments] ADD  DEFAULT (getdate()) FOR [UploadedAt]
ALTER TABLE [dbo].[Attachments]  WITH NOCHECK ADD FOREIGN KEY([UploadedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL

GO

/****** Object:  Table [dbo].[Balloting]    Script Date: 5/8/2026 11:22:12 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Balloting](
	[BallotID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ConductedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ConductedAt] [datetime] NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[BallotID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Balloting] ADD  DEFAULT (getdate()) FOR [ConductedAt]
ALTER TABLE [dbo].[Balloting]  WITH NOCHECK ADD FOREIGN KEY([ConductedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Balloting]  WITH NOCHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])

GO

/****** Object:  Table [dbo].[Ballotings]    Script Date: 5/8/2026 11:22:12 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Ballotings](
	[BallotingID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BallotingDate] [datetime] NOT NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ConductedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BallotingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Ballotings] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Ballotings]  WITH NOCHECK ADD FOREIGN KEY([ConductedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Ballotings]  WITH NOCHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
ON DELETE SET NULL

GO

/****** Object:  Table [dbo].[BlockingLogs]    Script Date: 5/8/2026 11:22:13 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[BlockingLogs](
	[BlockingLogID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UserID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ActionDate] [datetime] NULL,
	[PreviousStatus] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NewStatus] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Reason] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AttachmentPath] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[BlockingLogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[BlockingLogs] ADD  DEFAULT (getdate()) FOR [ActionDate]
ALTER TABLE [dbo].[BlockingLogs]  WITH CHECK ADD  CONSTRAINT [FK_BlockingLogs_Customer] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[BlockingLogs] CHECK CONSTRAINT [FK_BlockingLogs_Customer]
ALTER TABLE [dbo].[BlockingLogs]  WITH CHECK ADD  CONSTRAINT [FK_BlockingLogs_User] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[BlockingLogs] CHECK CONSTRAINT [FK_BlockingLogs_User]

GO

/****** Object:  Table [dbo].[Configuration]    Script Date: 5/8/2026 11:22:13 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Configuration](
	[ConfigKey] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Category] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ConfigValue] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[UpdatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[ConfigKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Configuration] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Configuration]  WITH NOCHECK ADD  CONSTRAINT [FK_Configuration_Users_UpdatedBy] FOREIGN KEY([UpdatedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Configuration] CHECK CONSTRAINT [FK_Configuration_Users_UpdatedBy]

GO

/****** Object:  Table [dbo].[CustomerLogs]    Script Date: 5/8/2026 11:22:13 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[CustomerLogs](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Action] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[CustomerLogs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[CustomerLogs]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])

GO

/****** Object:  Table [dbo].[Customers]    Script Date: 5/8/2026 11:22:14 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Customers](
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RegID] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PlanID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FullName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FatherName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CNIC] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PassportNo] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DOB] [date] NULL,
	[Gender] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Phone] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Email] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MailingAddress] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PermanentAddress] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[City] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Country] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SubProject] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RegisteredSize] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NomineeName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NomineeID] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NomineeRelation] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AdditionalInfo] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DealerID] [int] NULL,
	[ProjectID] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Nationality] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NomineeNICDocumentPath] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NomineePicturePath] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DealerName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[isDealerRegistered] [int] NULL,
	[MobileNo] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MobileNo2] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FormNo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DealerPercentage] [decimal](5, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Customers] ADD  DEFAULT (N'') FOR [Phone]
ALTER TABLE [dbo].[Customers] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Customers] ADD  CONSTRAINT [DF_Customers_Status_Default]  DEFAULT ('Pending') FOR [Status]
ALTER TABLE [dbo].[Customers] ADD  DEFAULT (N'') FOR [MobileNo]
ALTER TABLE [dbo].[Customers]  WITH NOCHECK ADD FOREIGN KEY([PlanID])
REFERENCES [dbo].[PaymentPlan] ([PlanID])
ALTER TABLE [dbo].[Customers]  WITH NOCHECK ADD FOREIGN KEY([RegID])
REFERENCES [dbo].[Registration] ([RegID])
ON DELETE SET NULL

GO

/****** Object:  Table [dbo].[CustomerUpdateRequestChanges]    Script Date: 5/8/2026 11:22:14 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[CustomerUpdateRequestChanges](
	[Id] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RequestID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FieldName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OldValue] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NewValue] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_CustomerUpdateRequestChanges_RequestID]    Script Date: 5/8/2026 11:22:14 AM ******/
CREATE NONCLUSTERED INDEX [IX_CustomerUpdateRequestChanges_RequestID] ON [dbo].[CustomerUpdateRequestChanges]
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[CustomerUpdateRequestChanges] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[CustomerUpdateRequestChanges]  WITH CHECK ADD  CONSTRAINT [FK_CustomerUpdateRequestChanges_Requests] FOREIGN KEY([RequestID])
REFERENCES [dbo].[CustomerUpdateRequests] ([RequestID])
ON DELETE CASCADE
ALTER TABLE [dbo].[CustomerUpdateRequestChanges] CHECK CONSTRAINT [FK_CustomerUpdateRequestChanges_Requests]

GO

/****** Object:  Table [dbo].[CustomerUpdateRequests]    Script Date: 5/8/2026 11:22:14 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[CustomerUpdateRequests](
	[RequestID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Status] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProposedDataJson] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OriginalDataJson] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReviewerComments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestedAt] [datetime] NOT NULL,
	[ApprovedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApprovedAt] [datetime] NULL,
	[RejectedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RejectedAt] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_CustomerUpdateRequests_Customer_Status]    Script Date: 5/8/2026 11:22:14 AM ******/
CREATE NONCLUSTERED INDEX [IX_CustomerUpdateRequests_Customer_Status] ON [dbo].[CustomerUpdateRequests]
(
	[CustomerID] ASC,
	[Status] ASC,
	[RequestedAt] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[CustomerUpdateRequests] ADD  DEFAULT ('Pending') FOR [Status]
ALTER TABLE [dbo].[CustomerUpdateRequests] ADD  DEFAULT (getdate()) FOR [RequestedAt]
ALTER TABLE [dbo].[CustomerUpdateRequests] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[CustomerUpdateRequests]  WITH CHECK ADD  CONSTRAINT [FK_CustomerUpdateRequests_Customers] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[CustomerUpdateRequests] CHECK CONSTRAINT [FK_CustomerUpdateRequests_Customers]

GO

/****** Object:  Table [dbo].[DealerPayments]    Script Date: 5/8/2026 11:22:15 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[DealerPayments](
	[Id] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DealerID] [int] NOT NULL,
	[TotalAmountDue] [decimal](18, 2) NOT NULL,
	[AmountPaid] [decimal](18, 2) NOT NULL,
	[Balance] [decimal](18, 2) NOT NULL,
	[Date] [datetime] NOT NULL,
	[PaymentMethod] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InstrumentNo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BankName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaymentHandedTo] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ChequeDate] [date] NULL,
	[PaymentDetails] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[created_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[created_at] [datetime] NOT NULL,
	[modified_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[modified_at] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
/****** Object:  Index [IX_DealerPayments_DealerID_Date]    Script Date: 5/8/2026 11:22:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_DealerPayments_DealerID_Date] ON [dbo].[DealerPayments]
(
	[DealerID] ASC,
	[Date] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[DealerPayments] ADD  DEFAULT (getdate()) FOR [Date]
ALTER TABLE [dbo].[DealerPayments] ADD  DEFAULT (getdate()) FOR [created_at]
ALTER TABLE [dbo].[DealerPayments]  WITH CHECK ADD  CONSTRAINT [FK_DealerPayments_Dealers] FOREIGN KEY([DealerID])
REFERENCES [dbo].[Dealers] ([DealerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[DealerPayments] CHECK CONSTRAINT [FK_DealerPayments_Dealers]

GO

/****** Object:  Table [dbo].[Dealers]    Script Date: 5/8/2026 11:22:15 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Dealers](
	[DealerID] [int] IDENTITY(1,1) NOT NULL,
	[DealershipName] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RegisterationDate] [date] NOT NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MembershipType] [nchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OwnerName] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OwnerCNIC] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MobileNo] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PhoneNumber] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Email] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Address] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OwnerDetails] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Details] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IncentivePercentage] [float] NULL,
PRIMARY KEY CLUSTERED 
(
	[DealerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[Dealers] ADD  DEFAULT ((5.0)) FOR [IncentivePercentage]

GO

/****** Object:  Table [dbo].[DuplicateFileTransfer]    Script Date: 5/8/2026 11:22:15 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[DuplicateFileTransfer](
	[Id] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CustomerCNIC] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Created_at] [datetime] NOT NULL,
	[Created_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Modified_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Comments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FeeDue] [decimal](18, 2) NULL,
	[FeePaid] [decimal](18, 2) NULL,
	[ChallanID] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BankName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InstrumentNo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DepositDate] [date] NULL,
	[PaymentMethod] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_DuplicateFileTransfer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[DuplicateFileTransfer] ADD  CONSTRAINT [DF_DuplicateFileTransfer_Created_at]  DEFAULT (getdate()) FOR [Created_at]
ALTER TABLE [dbo].[DuplicateFileTransfer]  WITH CHECK ADD  CONSTRAINT [FK_DuplicateFileTransfer_Customers] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[DuplicateFileTransfer] CHECK CONSTRAINT [FK_DuplicateFileTransfer_Customers]

GO

/****** Object:  Table [dbo].[JointOwner]    Script Date: 5/8/2026 11:22:15 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[JointOwner](
	[Id] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[JointOwnerName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CNIC] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Contact] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Address] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Percentage] [decimal](5, 2) NULL,
	[Created_at] [datetime] NOT NULL,
	[Created_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Modified_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Details] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FatherName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_JointOwner_CustomerID]    Script Date: 5/8/2026 11:22:15 AM ******/
CREATE NONCLUSTERED INDEX [IX_JointOwner_CustomerID] ON [dbo].[JointOwner]
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[JointOwner] ADD  CONSTRAINT [DF_JointOwner_CreatedAt]  DEFAULT (getdate()) FOR [Created_at]
ALTER TABLE [dbo].[JointOwner]  WITH CHECK ADD  CONSTRAINT [FK_JointOwner_Customers] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[JointOwner] CHECK CONSTRAINT [FK_JointOwner_Customers]

GO

/****** Object:  Table [dbo].[NDC]    Script Date: 5/8/2026 11:22:16 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[NDC](
	[NDCID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NDCType] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Title] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WorkFlowStatus] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Comments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IssuedDate] [datetime2](7) NOT NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NDCExpiryDate] [date] NULL,
	[TotalDueAmount] [decimal](18, 2) NULL,
	[TotalDueInstallments] [decimal](18, 2) NULL,
	[AllPaymentClear] [bit] NOT NULL,
	[AmountPerUnit] [decimal](18, 2) NULL,
	[PropertySize] [decimal](18, 2) NULL,
	[TransferFeeAmount] [decimal](18, 2) NULL,
	[RemainingDues] [decimal](18, 2) NULL,
 CONSTRAINT [PK_NDC] PRIMARY KEY CLUSTERED 
(
	[NDCID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_NDC_CustomerID]    Script Date: 5/8/2026 11:22:16 AM ******/
CREATE NONCLUSTERED INDEX [IX_NDC_CustomerID] ON [dbo].[NDC]
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[NDC] ADD  DEFAULT (getdate()) FOR [IssuedDate]
ALTER TABLE [dbo].[NDC] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[NDC] ADD  DEFAULT ((0)) FOR [AllPaymentClear]
ALTER TABLE [dbo].[NDC]  WITH CHECK ADD  CONSTRAINT [FK_NDC_Customers_CustomerID] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[NDC] CHECK CONSTRAINT [FK_NDC_Customers_CustomerID]

GO

/****** Object:  Table [dbo].[NDCs]    Script Date: 5/8/2026 11:22:16 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[NDCs](
	[NDCID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IssuedDate] [datetime] NOT NULL,
	[ExpiryDate] [datetime] NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IssuedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NDCID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[NDCs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[NDCs]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[NDCs]  WITH NOCHECK ADD FOREIGN KEY([IssuedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[NDCs]  WITH NOCHECK ADD FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])
ON DELETE SET NULL

GO

/****** Object:  Table [dbo].[Notifications]    Script Date: 5/8/2026 11:22:16 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Notifications](
	[NotificationID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UserID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Message] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT ((0)) FOR [IsRead]
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Notifications]  WITH NOCHECK ADD FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE

GO

/****** Object:  Table [dbo].[PaymentPlan]    Script Date: 5/8/2026 11:22:16 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PaymentPlan](
	[PlanID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PlanName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[DurationMonths] [int] NULL,
	[Frequency] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[Currency] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ExchangeRate] [decimal](18, 4) NULL,
	[TotalAmountUSD] [decimal](18, 2) NULL,
	[RegisteredSize] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SubProject] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[PlanID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[PaymentPlan] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[PaymentPlan] ADD  DEFAULT ('SSP') FOR [Currency]

GO

/****** Object:  Table [dbo].[Payments]    Script Date: 5/8/2026 11:22:17 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Payments](
	[PaymentID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ScheduleID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[PaymentDate] [datetime] NOT NULL,
	[PaymentMethod] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReferenceNumber] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
	[AuditRemarks] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AuditStatus] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AuditedAt] [datetime2](7) NULL,
	[AuditedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AccountHead] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DepositDate] [date] NULL,
	[BankName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[PaymentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_Payments_AuditedBy]    Script Date: 5/8/2026 11:22:17 AM ******/
CREATE NONCLUSTERED INDEX [IX_Payments_AuditedBy] ON [dbo].[Payments]
(
	[AuditedBy] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Payments] ADD  DEFAULT ('Pending') FOR [Status]
ALTER TABLE [dbo].[Payments] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Payments] ADD  DEFAULT (N'Pending') FOR [AuditStatus]
ALTER TABLE [dbo].[Payments]  WITH NOCHECK ADD FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Payments]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[Payments]  WITH NOCHECK ADD FOREIGN KEY([ScheduleID])
REFERENCES [dbo].[PaymentSchedule] ([ScheduleID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [FK_Payments_Users_AuditedBy] FOREIGN KEY([AuditedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [FK_Payments_Users_AuditedBy]
/****** Object:  Trigger [dbo].[TRG_Payments_ValidateBankReferenceCrossCustomer]    Script Date: 5/8/2026 11:22:17 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER OFF
CREATE   TRIGGER TRG_Payments_ValidateBankReferenceCrossCustomer ON Payments AFTER INSERT, UPDATE AS BEGIN SET NOCOUNT ON; IF EXISTS ( SELECT 1 FROM inserted i JOIN Payments p ON p.PaymentID <> i.PaymentID AND ISNULL(LTRIM(RTRIM(i.BankName)),'') <> '' AND ISNULL(LTRIM(RTRIM(i.ReferenceNumber)),'') <> '' AND ISNULL(LTRIM(RTRIM(p.BankName)),'') = LTRIM(RTRIM(i.BankName)) AND ISNULL(LTRIM(RTRIM(p.ReferenceNumber)),'') = LTRIM(RTRIM(i.ReferenceNumber)) AND ISNULL(LTRIM(RTRIM(p.CustomerID)),'') <> ISNULL(LTRIM(RTRIM(i.CustomerID)),'') ) BEGIN RAISERROR('The combination of Bank Name and Reference Number is already used by another customer.',16,1); ROLLBACK TRANSACTION; RETURN; END END;
ALTER TABLE [dbo].[Payments] ENABLE TRIGGER [TRG_Payments_ValidateBankReferenceCrossCustomer]

GO

/****** Object:  Table [dbo].[PaymentSchedule]    Script Date: 5/8/2026 11:22:22 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PaymentSchedule](
	[ScheduleID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PlanID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaymentDescription] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InstallmentNo] [int] NULL,
	[DueDate] [date] NULL,
	[Amount] [decimal](18, 2) NULL,
	[SurchargeApplied] [bit] NULL,
	[SurchargeRate] [decimal](18, 6) NOT NULL,
	[Description] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AmountUSD] [decimal](18, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[ScheduleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[PaymentSchedule] ADD  DEFAULT ((1)) FOR [SurchargeApplied]
ALTER TABLE [dbo].[PaymentSchedule]  WITH NOCHECK ADD FOREIGN KEY([PlanID])
REFERENCES [dbo].[PaymentPlan] ([PlanID])

GO

/****** Object:  Table [dbo].[Penalties]    Script Date: 5/8/2026 11:22:22 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Penalties](
	[PenaltyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[PenaltyDate] [datetime] NOT NULL,
	[PenaltyReason] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PenaltyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Penalties] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Penalties]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE

GO

/****** Object:  Table [dbo].[Possession]    Script Date: 5/8/2026 11:22:22 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Possession](
	[PossessionID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PossessionDate] [datetime] NULL,
	[WorkFlowStatus] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Comments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[PossessionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Possession] ADD  DEFAULT (getdate()) FOR [PossessionDate]
ALTER TABLE [dbo].[Possession]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Possession]  WITH NOCHECK ADD FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])

GO

/****** Object:  Table [dbo].[Possessions]    Script Date: 5/8/2026 11:22:23 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Possessions](
	[PossessionID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PossessionDate] [datetime] NOT NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PossessionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Possessions] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Possessions]  WITH NOCHECK ADD  CONSTRAINT [FK_Possessions_Customer] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Possessions] NOCHECK CONSTRAINT [FK_Possessions_Customer]
ALTER TABLE [dbo].[Possessions]  WITH NOCHECK ADD  CONSTRAINT [FK_Possessions_Property] FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])
ALTER TABLE [dbo].[Possessions] NOCHECK CONSTRAINT [FK_Possessions_Property]

GO

/****** Object:  Table [dbo].[Projects]    Script Date: 5/8/2026 11:22:23 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Projects](
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Prefix] [nvarchar](7) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Location] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[Sizes] [nvarchar](1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PropertyTypes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SubProjects] [nvarchar](1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Projects] ADD  DEFAULT (getdate()) FOR [CreatedAt]

GO

/****** Object:  Table [dbo].[ProjectSubProjects]    Script Date: 5/8/2026 11:22:23 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
