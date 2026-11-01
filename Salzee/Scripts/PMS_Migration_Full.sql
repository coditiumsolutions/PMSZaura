-- ====================================================================
-- PMS Migration SQL (schema + data)
-- Generated: 2026-05-05 11:04:36
-- Source: localhost / PMSAbbas
-- ====================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'PMSAbbas') IS NULL
BEGIN
    CREATE DATABASE [PMSAbbas];
END
GO
USE [PMSAbbas];
GO


-- ==================== SCHEMA ====================

/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 5/5/2026 11:12:15 AM ******/
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

/****** Object:  Table [dbo].[ACL]    Script Date: 5/5/2026 11:12:16 AM ******/
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

/****** Object:  Table [dbo].[ActivityLog]    Script Date: 5/5/2026 11:12:16 AM ******/
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

/****** Object:  Table [dbo].[Allotment]    Script Date: 5/5/2026 11:12:17 AM ******/
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

/****** Object:  Table [dbo].[Allotments]    Script Date: 5/5/2026 11:12:17 AM ******/
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

/****** Object:  Table [dbo].[Approvals]    Script Date: 5/5/2026 11:12:17 AM ******/
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

/****** Object:  Table [dbo].[Attachments]    Script Date: 5/5/2026 11:12:17 AM ******/
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

/****** Object:  Table [dbo].[Balloting]    Script Date: 5/5/2026 11:12:18 AM ******/
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

/****** Object:  Table [dbo].[Ballotings]    Script Date: 5/5/2026 11:12:18 AM ******/
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

/****** Object:  Table [dbo].[BlockingLogs]    Script Date: 5/5/2026 11:12:19 AM ******/
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

/****** Object:  Table [dbo].[Configuration]    Script Date: 5/5/2026 11:12:19 AM ******/
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

/****** Object:  Table [dbo].[CustomerLogs]    Script Date: 5/5/2026 11:12:19 AM ******/
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

/****** Object:  Table [dbo].[Customers]    Script Date: 5/5/2026 11:12:20 AM ******/
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

/****** Object:  Table [dbo].[Dealers]    Script Date: 5/5/2026 11:12:20 AM ******/
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

/****** Object:  Table [dbo].[DuplicateFileTransfer]    Script Date: 5/5/2026 11:12:20 AM ******/
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

/****** Object:  Table [dbo].[JointOwner]    Script Date: 5/5/2026 11:12:20 AM ******/
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

/****** Object:  Index [IX_JointOwner_CustomerID]    Script Date: 5/5/2026 11:12:20 AM ******/
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

/****** Object:  Table [dbo].[NDC]    Script Date: 5/5/2026 11:12:21 AM ******/
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

/****** Object:  Index [IX_NDC_CustomerID]    Script Date: 5/5/2026 11:12:21 AM ******/
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

/****** Object:  Table [dbo].[NDCs]    Script Date: 5/5/2026 11:12:21 AM ******/
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

/****** Object:  Table [dbo].[Notifications]    Script Date: 5/5/2026 11:12:21 AM ******/
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

/****** Object:  Table [dbo].[PaymentPlan]    Script Date: 5/5/2026 11:12:21 AM ******/
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

/****** Object:  Table [dbo].[Payments]    Script Date: 5/5/2026 11:12:21 AM ******/
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

/****** Object:  Index [IX_Payments_AuditedBy]    Script Date: 5/5/2026 11:12:21 AM ******/
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
/****** Object:  Trigger [dbo].[TRG_Payments_ValidateBankReferenceCrossCustomer]    Script Date: 5/5/2026 11:12:21 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER OFF
CREATE   TRIGGER TRG_Payments_ValidateBankReferenceCrossCustomer ON Payments AFTER INSERT, UPDATE AS BEGIN SET NOCOUNT ON; IF EXISTS ( SELECT 1 FROM inserted i JOIN Payments p ON p.PaymentID <> i.PaymentID AND ISNULL(LTRIM(RTRIM(i.BankName)),'') <> '' AND ISNULL(LTRIM(RTRIM(i.ReferenceNumber)),'') <> '' AND ISNULL(LTRIM(RTRIM(p.BankName)),'') = LTRIM(RTRIM(i.BankName)) AND ISNULL(LTRIM(RTRIM(p.ReferenceNumber)),'') = LTRIM(RTRIM(i.ReferenceNumber)) AND ISNULL(LTRIM(RTRIM(p.CustomerID)),'') <> ISNULL(LTRIM(RTRIM(i.CustomerID)),'') ) BEGIN RAISERROR('The combination of Bank Name and Reference Number is already used by another customer.',16,1); ROLLBACK TRANSACTION; RETURN; END END;
ALTER TABLE [dbo].[Payments] ENABLE TRIGGER [TRG_Payments_ValidateBankReferenceCrossCustomer]

GO

/****** Object:  Table [dbo].[PaymentSchedule]    Script Date: 5/5/2026 11:12:23 AM ******/
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

/****** Object:  Table [dbo].[Penalties]    Script Date: 5/5/2026 11:12:23 AM ******/
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

/****** Object:  Table [dbo].[Possession]    Script Date: 5/5/2026 11:12:24 AM ******/
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

/****** Object:  Table [dbo].[Possessions]    Script Date: 5/5/2026 11:12:24 AM ******/
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

/****** Object:  Table [dbo].[Projects]    Script Date: 5/5/2026 11:12:24 AM ******/
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

/****** Object:  Table [dbo].[ProjectSubProjects]    Script Date: 5/5/2026 11:12:24 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[ProjectSubProjects](
	[Id] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SubProjectName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Prefix] [nvarchar](7) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [UX_ProjectSubProjects_Project_Prefix]    Script Date: 5/5/2026 11:12:24 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_ProjectSubProjects_Project_Prefix] ON [dbo].[ProjectSubProjects]
(
	[ProjectID] ASC,
	[Prefix] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [UX_ProjectSubProjects_Project_SubProjectName]    Script Date: 5/5/2026 11:12:24 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_ProjectSubProjects_Project_SubProjectName] ON [dbo].[ProjectSubProjects]
(
	[ProjectID] ASC,
	[SubProjectName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[ProjectSubProjects] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[ProjectSubProjects]  WITH CHECK ADD  CONSTRAINT [FK_ProjectSubProjects_Projects_ProjectID] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
ON DELETE CASCADE
ALTER TABLE [dbo].[ProjectSubProjects] CHECK CONSTRAINT [FK_ProjectSubProjects_Projects_ProjectID]

GO

/****** Object:  Table [dbo].[Property]    Script Date: 5/5/2026 11:12:24 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Property](
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PlotNo] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Street] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PlotType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Block] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PropertyType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Size] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[AdditionalInfo] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DealerID] [int] NULL,
	[Floor] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SubProject] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Property] ADD  DEFAULT ('Available') FOR [Status]
ALTER TABLE [dbo].[Property] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Property]  WITH NOCHECK ADD FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])

GO

/****** Object:  Table [dbo].[PropertyInquiry]    Script Date: 5/5/2026 11:12:25 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[PropertyInquiry](
	[InquiryID] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PhoneNumber] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[EmailAddress] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[InquiryType] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Message] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SubmittedAt] [datetime] NULL,
	[IPAddress] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssignedTo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FollowUpDate] [datetime] NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsContacted] [bit] NULL,
	[CreatedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[InquiryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[PropertyInquiry] ADD  DEFAULT (getdate()) FOR [SubmittedAt]
ALTER TABLE [dbo].[PropertyInquiry] ADD  DEFAULT ('New') FOR [Status]
ALTER TABLE [dbo].[PropertyInquiry] ADD  DEFAULT ((0)) FOR [IsContacted]
ALTER TABLE [dbo].[PropertyInquiry] ADD  DEFAULT (getdate()) FOR [CreatedAt]

GO

/****** Object:  Table [dbo].[PropertyLogs]    Script Date: 5/5/2026 11:12:25 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[PropertyLogs](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Action] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OldValue] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NewValue] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[PropertyLogs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[PropertyLogs]  WITH NOCHECK ADD  CONSTRAINT [FK_PropertyLogs_Property] FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])
ALTER TABLE [dbo].[PropertyLogs] NOCHECK CONSTRAINT [FK_PropertyLogs_Property]
ALTER TABLE [dbo].[PropertyLogs]  WITH NOCHECK ADD  CONSTRAINT [FK_PropertyLogs_Users] FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[PropertyLogs] NOCHECK CONSTRAINT [FK_PropertyLogs_Users]

GO

/****** Object:  Table [dbo].[Refund]    Script Date: 5/5/2026 11:12:26 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Refund](
	[RefundID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RefundType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaidAmount] [decimal](18, 2) NOT NULL,
	[DeductionAmount] [decimal](18, 2) NOT NULL,
	[RefundedAmount] [decimal](18, 2) NOT NULL,
	[Reason] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WorkflowStatus] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SelectedPaymentIDs] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[ApprovedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[Notes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Refund] PRIMARY KEY CLUSTERED 
(
	[RefundID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_Refund_ApprovedBy]    Script Date: 5/5/2026 11:12:26 AM ******/
CREATE NONCLUSTERED INDEX [IX_Refund_ApprovedBy] ON [dbo].[Refund]
(
	[ApprovedBy] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_Refund_CreatedBy]    Script Date: 5/5/2026 11:12:26 AM ******/
CREATE NONCLUSTERED INDEX [IX_Refund_CreatedBy] ON [dbo].[Refund]
(
	[CreatedBy] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_Refund_CustomerID]    Script Date: 5/5/2026 11:12:26 AM ******/
CREATE NONCLUSTERED INDEX [IX_Refund_CustomerID] ON [dbo].[Refund]
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Refund] ADD  DEFAULT ((0.0)) FOR [PaidAmount]
ALTER TABLE [dbo].[Refund] ADD  DEFAULT ((0.0)) FOR [DeductionAmount]
ALTER TABLE [dbo].[Refund] ADD  DEFAULT ((0.0)) FOR [RefundedAmount]
ALTER TABLE [dbo].[Refund] ADD  DEFAULT (N'Initiated') FOR [WorkflowStatus]
ALTER TABLE [dbo].[Refund] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Refund]  WITH CHECK ADD  CONSTRAINT [FK_Refund_Customers_CustomerID] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[Refund] CHECK CONSTRAINT [FK_Refund_Customers_CustomerID]
ALTER TABLE [dbo].[Refund]  WITH CHECK ADD  CONSTRAINT [FK_Refund_Users_ApprovedBy] FOREIGN KEY([ApprovedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Refund] CHECK CONSTRAINT [FK_Refund_Users_ApprovedBy]
ALTER TABLE [dbo].[Refund]  WITH CHECK ADD  CONSTRAINT [FK_Refund_Users_CreatedBy] FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Refund] CHECK CONSTRAINT [FK_Refund_Users_CreatedBy]

GO

/****** Object:  Table [dbo].[RefundCheques]    Script Date: 5/5/2026 11:12:26 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[RefundCheques](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RefundID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ChequeNo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ChequeDate] [date] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Bank] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Details] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[created_at] [datetime2](3) NOT NULL,
	[created_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[modified_by] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_RefundCheques] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_RefundCheques_RefundID]    Script Date: 5/5/2026 11:12:26 AM ******/
CREATE NONCLUSTERED INDEX [IX_RefundCheques_RefundID] ON [dbo].[RefundCheques]
(
	[RefundID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[RefundCheques] ADD  CONSTRAINT [DF_RefundCheques_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
ALTER TABLE [dbo].[RefundCheques]  WITH CHECK ADD  CONSTRAINT [FK_RefundCheques_Refund] FOREIGN KEY([RefundID])
REFERENCES [dbo].[Refund] ([RefundID])
ON DELETE CASCADE
ALTER TABLE [dbo].[RefundCheques] CHECK CONSTRAINT [FK_RefundCheques_Refund]

GO

/****** Object:  Table [dbo].[Refunds]    Script Date: 5/5/2026 11:12:26 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Refunds](
	[RefundID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[RefundDate] [datetime] NOT NULL,
	[RefundReason] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApprovedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RefundID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Refunds] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Refunds]  WITH NOCHECK ADD FOREIGN KEY([ApprovedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Refunds]  WITH NOCHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE

GO

/****** Object:  Table [dbo].[Registration]    Script Date: 5/5/2026 11:12:27 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Registration](
	[RegID] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FullName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CNIC] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Phone] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Email] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Size] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PassportNo] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SubProject] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FormNo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[RegID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_Registration_ProjectID]    Script Date: 5/5/2026 11:12:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_Registration_ProjectID] ON [dbo].[Registration]
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Registration] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Registration] ADD  DEFAULT ('Pending') FOR [Status]
ALTER TABLE [dbo].[Registration]  WITH CHECK ADD  CONSTRAINT [FK_Registration_Projects_ProjectID] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Registration] CHECK CONSTRAINT [FK_Registration_Projects_ProjectID]

GO

/****** Object:  Table [dbo].[Rental]    Script Date: 5/5/2026 11:12:27 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Rental](
	[RentalID] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TenantName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TenantCNIC] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TenantPhone] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TenantEmail] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TenantAddress] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MonthlyRent] [decimal](18, 2) NOT NULL,
	[SecurityDeposit] [decimal](18, 2) NULL,
	[AdvanceRent] [decimal](18, 2) NULL,
	[Currency] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[StartDate] [date] NOT NULL,
	[EndDate] [date] NULL,
	[DurationMonths] [int] NOT NULL,
	[PaymentDueDayOfMonth] [int] NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Rental] PRIMARY KEY CLUSTERED 
(
	[RentalID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_Rental_PropertyID]    Script Date: 5/5/2026 11:12:27 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Rental_PropertyID] ON [dbo].[Rental]
(
	[PropertyID] ASC
)
WHERE ([Status]='Active')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_Rental_Status_StartDate]    Script Date: 5/5/2026 11:12:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_Rental_Status_StartDate] ON [dbo].[Rental]
(
	[Status] ASC,
	[StartDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Rental] ADD  DEFAULT (N'PKR') FOR [Currency]
ALTER TABLE [dbo].[Rental] ADD  DEFAULT (N'Active') FOR [Status]
ALTER TABLE [dbo].[Rental]  WITH CHECK ADD  CONSTRAINT [FK_Rental_Property_PropertyID] FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])
ALTER TABLE [dbo].[Rental] CHECK CONSTRAINT [FK_Rental_Property_PropertyID]

GO

/****** Object:  Table [dbo].[RentalPayments]    Script Date: 5/5/2026 11:12:27 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[RentalPayments](
	[RentalPaymentID] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RentalID] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[BillingYear] [int] NOT NULL,
	[BillingMonth] [int] NOT NULL,
	[DueDate] [date] NOT NULL,
	[AmountDue] [decimal](18, 2) NOT NULL,
	[AmountPaid] [decimal](18, 2) NOT NULL,
	[PaidOn] [datetime2](7) NULL,
	[PaymentMethod] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReferenceNo] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RentalPayments] PRIMARY KEY CLUSTERED 
(
	[RentalPaymentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

/****** Object:  Index [IX_RentalPayments_BillingYear_BillingMonth]    Script Date: 5/5/2026 11:12:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_RentalPayments_BillingYear_BillingMonth] ON [dbo].[RentalPayments]
(
	[BillingYear] ASC,
	[BillingMonth] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_RentalPayments_RentalID]    Script Date: 5/5/2026 11:12:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_RentalPayments_RentalID] ON [dbo].[RentalPayments]
(
	[RentalID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_RentalPayments_Status_DueDate]    Script Date: 5/5/2026 11:12:27 AM ******/
CREATE NONCLUSTERED INDEX [IX_RentalPayments_Status_DueDate] ON [dbo].[RentalPayments]
(
	[Status] ASC,
	[DueDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[RentalPayments] ADD  DEFAULT (N'Pending') FOR [Status]
ALTER TABLE [dbo].[RentalPayments]  WITH CHECK ADD  CONSTRAINT [FK_RentalPayments_Rental_RentalID] FOREIGN KEY([RentalID])
REFERENCES [dbo].[Rental] ([RentalID])
ON DELETE CASCADE
ALTER TABLE [dbo].[RentalPayments] CHECK CONSTRAINT [FK_RentalPayments_Rental_RentalID]

GO

/****** Object:  Table [dbo].[Tickets]    Script Date: 5/5/2026 11:12:27 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Tickets](
	[TicketID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Email] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Contact] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Title] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CROComments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedBy] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AssignedTo] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TicketClosingDate] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Tickets] PRIMARY KEY CLUSTERED 
(
	[TicketID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Tickets] ADD  DEFAULT (getdate()) FOR [CreatedAt]

GO

/****** Object:  Table [dbo].[Transfer]    Script Date: 5/5/2026 11:12:28 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Transfer](
	[TransferID] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WorkFlowStatus] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[SellerName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SellerFatherName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SellerCNIC] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SellerContact] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SellerAddress] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerFatherName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerCNIC] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerContact] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerAddress] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerCity] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerCountry] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerAttachments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SellerAttachments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TransferFeeDue] [float] NULL,
	[TransferFeePaid] [float] NULL,
	[PaymentDate] [date] NULL,
	[PaymentMode] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaymentChallanNo] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Details] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CROComments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AccountsComments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TransferComments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerBiometric] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SellerBiometric] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerPassportNo] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerDOB] [date] NULL,
	[BuyerEmail] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerGender] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerMailingAddress] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerMobile] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerMobile2] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerNationality] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerPermanentAddress] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BuyerPhone] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaymentMethod] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BankName] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaymentDetails] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Transfer] PRIMARY KEY CLUSTERED 
(
	[TransferID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_Transfer_CustomerID]    Script Date: 5/5/2026 11:12:28 AM ******/
CREATE NONCLUSTERED INDEX [IX_Transfer_CustomerID] ON [dbo].[Transfer]
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_Transfer_WorkFlowStatus]    Script Date: 5/5/2026 11:12:28 AM ******/
CREATE NONCLUSTERED INDEX [IX_Transfer_WorkFlowStatus] ON [dbo].[Transfer]
(
	[WorkFlowStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Transfer]  WITH CHECK ADD  CONSTRAINT [FK_Transfer_Customers_CustomerID] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Transfer] CHECK CONSTRAINT [FK_Transfer_Customers_CustomerID]

GO

/****** Object:  Table [dbo].[TransferFee]    Script Date: 5/5/2026 11:12:28 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[TransferFee](
	[Id] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProjectID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SubProject] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TransferType] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TransferPriority] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AmountPerUnit] [decimal](18, 2) NOT NULL,
	[CreatedOn] [datetime2](7) NOT NULL,
	[CreatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ModifiedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Details] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_TransferFee] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
SET ANSI_PADDING ON

/****** Object:  Index [IX_TransferFee_ProjectID]    Script Date: 5/5/2026 11:12:28 AM ******/
CREATE NONCLUSTERED INDEX [IX_TransferFee_ProjectID] ON [dbo].[TransferFee]
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[TransferFee]  WITH CHECK ADD  CONSTRAINT [FK_TransferFee_Projects_ProjectID] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[Projects] ([ProjectID])
ON DELETE CASCADE
ALTER TABLE [dbo].[TransferFee] CHECK CONSTRAINT [FK_TransferFee_Projects_ProjectID]

GO

/****** Object:  Table [dbo].[TransferJointOwners]    Script Date: 5/5/2026 11:12:28 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[TransferJointOwners](
	[Id] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TransferID] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
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

/****** Object:  Index [IX_TransferJointOwners_CustomerID]    Script Date: 5/5/2026 11:12:28 AM ******/
CREATE NONCLUSTERED INDEX [IX_TransferJointOwners_CustomerID] ON [dbo].[TransferJointOwners]
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

/****** Object:  Index [IX_TransferJointOwners_TransferID]    Script Date: 5/5/2026 11:12:28 AM ******/
CREATE NONCLUSTERED INDEX [IX_TransferJointOwners_TransferID] ON [dbo].[TransferJointOwners]
(
	[TransferID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[TransferJointOwners] ADD  CONSTRAINT [DF_TransferJointOwners_CreatedAt]  DEFAULT (getdate()) FOR [Created_at]
ALTER TABLE [dbo].[TransferJointOwners]  WITH CHECK ADD  CONSTRAINT [FK_TransferJointOwners_Customers] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ON DELETE CASCADE
ALTER TABLE [dbo].[TransferJointOwners] CHECK CONSTRAINT [FK_TransferJointOwners_Customers]
ALTER TABLE [dbo].[TransferJointOwners]  WITH CHECK ADD  CONSTRAINT [FK_TransferJointOwners_Transfer] FOREIGN KEY([TransferID])
REFERENCES [dbo].[Transfer] ([TransferID])
ON DELETE CASCADE
ALTER TABLE [dbo].[TransferJointOwners] CHECK CONSTRAINT [FK_TransferJointOwners_Transfer]

GO

/****** Object:  Table [dbo].[Transfers]    Script Date: 5/5/2026 11:12:28 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Transfers](
	[TransferID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PropertyID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FromCustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ToCustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TransferDate] [datetime] NOT NULL,
	[TransferReason] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApprovedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Remarks] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TransferID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Transfers] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Transfers]  WITH NOCHECK ADD FOREIGN KEY([ApprovedBy])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE SET NULL
ALTER TABLE [dbo].[Transfers]  WITH NOCHECK ADD FOREIGN KEY([FromCustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Transfers]  WITH NOCHECK ADD FOREIGN KEY([PropertyID])
REFERENCES [dbo].[Property] ([PropertyID])
ON DELETE CASCADE
ALTER TABLE [dbo].[Transfers]  WITH NOCHECK ADD FOREIGN KEY([ToCustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])

GO

/****** Object:  Table [dbo].[UserModulePermission]    Script Date: 5/5/2026 11:12:29 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[UserModulePermission](
	[UserID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ModuleKey] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Permission] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_UserModulePermission] PRIMARY KEY CLUSTERED 
(
	[UserID] ASC,
	[ModuleKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[UserModulePermission]  WITH CHECK ADD  CONSTRAINT [FK_UserModulePermission_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
ALTER TABLE [dbo].[UserModulePermission] CHECK CONSTRAINT [FK_UserModulePermission_Users_UserID]

GO

/****** Object:  Table [dbo].[Users]    Script Date: 5/5/2026 11:12:29 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Users](
	[UserID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FullName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Email] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PasswordHash] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RoleID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsActive] [bit] NULL,
	[CreatedAt] [datetime] NULL,
	[Department] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Designation] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[UserType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [dbo].[Users] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Users]  WITH NOCHECK ADD FOREIGN KEY([RoleID])
REFERENCES [dbo].[ACL] ([RoleID])

GO

/****** Object:  Table [dbo].[UserSessions]    Script Date: 5/5/2026 11:12:29 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[UserSessions](
	[SessionID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UserID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LoginTime] [datetime] NULL,
	[LogoutTime] [datetime] NULL,
	[IPAddress] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DeviceInfo] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[SessionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[UserSessions] ADD  DEFAULT (getdate()) FOR [LoginTime]
ALTER TABLE [dbo].[UserSessions]  WITH NOCHECK ADD FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])

GO

/****** Object:  Table [dbo].[Waiver]    Script Date: 5/5/2026 11:12:29 AM ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
CREATE TABLE [dbo].[Waiver](
	[WaiverID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WaiverType] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CustomerID] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AccountHead] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[WaivedAmount] [decimal](18, 2) NULL,
	[WaivedPercentage] [decimal](18, 2) NULL,
	[Comments] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ApprovedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LastModifiedBy] [char](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
	[ApprovedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[WaiverID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

SET ANSI_PADDING OFF
ALTER TABLE [dbo].[Waiver] ADD  DEFAULT ('Surcharge Waiver') FOR [WaiverType]
ALTER TABLE [dbo].[Waiver] ADD  DEFAULT ('Initiated') FOR [Status]
ALTER TABLE [dbo].[Waiver] ADD  DEFAULT ('Waived Off') FOR [AccountHead]
ALTER TABLE [dbo].[Waiver] ADD  DEFAULT (getdate()) FOR [CreatedAt]
ALTER TABLE [dbo].[Waiver]  WITH CHECK ADD FOREIGN KEY([ApprovedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Waiver]  WITH CHECK ADD FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Users] ([UserID])
ALTER TABLE [dbo].[Waiver]  WITH CHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Waiver]  WITH CHECK ADD FOREIGN KEY([LastModifiedBy])
REFERENCES [dbo].[Users] ([UserID])

GO


GO

-- ===================== DATA =====================

EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

