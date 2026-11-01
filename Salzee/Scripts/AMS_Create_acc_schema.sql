/*
  AMS — create acc schema and tables (from AMS_plan.md §3).
  Idempotent: skips objects that already exist.
  Run against PMSAbbas (or your target DB). Ref: db.txt (dbo tables), dbAccounts.txt (module inventory).

  PMS integration types (align with dbo / EF Models):
  - User refs (CreatedBy, PostedBy, …): NVARCHAR(10) — dbo.Users.UserID
  - Customer / Payment / Project / Allotment / Refund / Schedule / Penalty / DealerPayments.Id: NVARCHAR(10)
  - dbo.Dealers.DealerID stays INT — acc.Voucher.PMSDealerID, DealerCommissionVoucher.DealerID remain INT
  - dbo.Transfer.TransferID NVARCHAR(50), dbo.RentalPayments PK NVARCHAR(50): PMSTransferID, PMSRentalPaymentID

  Column renames vs AMS_plan.md §3 (SQL parser compatibility):
  - acc.VoucherLine.LineNumber (plan: LineNo), acc.VoucherLine.FxAmount (plan: ForeignAmount)
  - acc.APBillLine.LineNumber (plan: LineNo)

  Brownfield: if acc tables already exist with INT user/PMS columns, run Scripts/AMS_Migrate_acc_int_to_nvarchar_pms.sql first.
*/
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'acc')
    EXEC(N'CREATE SCHEMA acc');
GO

/* MODULE A */
IF OBJECT_ID(N'acc.AccountCategory', N'U') IS NULL
CREATE TABLE acc.AccountCategory (
        AccountCategoryID   INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName        NVARCHAR(100)   NOT NULL,
        NatureType          NVARCHAR(20)    NOT NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

IF OBJECT_ID(N'acc.AccountHead', N'U') IS NULL
CREATE TABLE acc.AccountHead (
        AccountHeadID       INT IDENTITY(1,1) PRIMARY KEY,
        AccountCategoryID   INT             NOT NULL
            REFERENCES acc.AccountCategory(AccountCategoryID),
        ParentAccountHeadID INT             NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        AccountCode         NVARCHAR(30)    NOT NULL UNIQUE,
        AccountName         NVARCHAR(150)   NOT NULL,
        AccountLevel        TINYINT         NOT NULL DEFAULT 1,
        IsControlAccount    BIT             NOT NULL DEFAULT 0,
        AllowDirectPosting  BIT             NOT NULL DEFAULT 1,
        OpeningBalance      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        OpeningBalanceDate  DATE            NULL,
        OpeningBalanceType  NVARCHAR(10)    NULL,
        Description         NVARCHAR(500)   NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

/* MODULE B */
IF OBJECT_ID(N'acc.FiscalYear', N'U') IS NULL
CREATE TABLE acc.FiscalYear (
        FiscalYearID        INT IDENTITY(1,1) PRIMARY KEY,
        YearName            NVARCHAR(50)    NOT NULL,
        StartDate           DATE            NOT NULL,
        EndDate             DATE            NOT NULL,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Open',
        ClosedBy            NVARCHAR(10)    NULL,
        ClosedAt            DATETIME2       NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

IF OBJECT_ID(N'acc.AccountingPeriod', N'U') IS NULL
CREATE TABLE acc.AccountingPeriod (
        PeriodID            INT IDENTITY(1,1) PRIMARY KEY,
        FiscalYearID        INT             NOT NULL
            REFERENCES acc.FiscalYear(FiscalYearID),
        PeriodName          NVARCHAR(50)    NOT NULL,
        StartDate           DATE            NOT NULL,
        EndDate             DATE            NOT NULL,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Open',
        ClosedBy            NVARCHAR(10)    NULL,
        ClosedAt            DATETIME2       NULL
    );
GO

/* MODULE C — before VoucherLine FK */
IF OBJECT_ID(N'acc.CostCenter', N'U') IS NULL
CREATE TABLE acc.CostCenter (
        CostCenterID        INT IDENTITY(1,1) PRIMARY KEY,
        CostCenterCode      NVARCHAR(20)    NOT NULL UNIQUE,
        CostCenterName      NVARCHAR(150)   NOT NULL,
        ParentCostCenterID  INT             NULL
            REFERENCES acc.CostCenter(CostCenterID),
        ProjectID           NVARCHAR(10)    NULL,
        SubProjectID        NVARCHAR(10)    NULL,
        CostCenterType      NVARCHAR(30)    NULL,
        BudgetAmount        DECIMAL(18,2)   NULL DEFAULT 0,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

/* MODULE D */
IF OBJECT_ID(N'acc.VoucherType', N'U') IS NULL
CREATE TABLE acc.VoucherType (
        VoucherTypeID       INT IDENTITY(1,1) PRIMARY KEY,
        TypeCode            NVARCHAR(10)    NOT NULL UNIQUE,
        TypeName            NVARCHAR(100)   NOT NULL,
        Prefix              NVARCHAR(10)    NULL,
        IsAutoNumbered      BIT             NOT NULL DEFAULT 1,
        IsActive            BIT             NOT NULL DEFAULT 1
    );
GO

IF OBJECT_ID(N'acc.Voucher', N'U') IS NULL
CREATE TABLE acc.Voucher (
        VoucherID           INT IDENTITY(1,1) PRIMARY KEY,
        VoucherTypeID       INT             NOT NULL
            REFERENCES acc.VoucherType(VoucherTypeID),
        VoucherNo           NVARCHAR(30)    NOT NULL UNIQUE,
        VoucherDate         DATE            NOT NULL,
        PeriodID            INT             NOT NULL
            REFERENCES acc.AccountingPeriod(PeriodID),
        FiscalYearID        INT             NOT NULL
            REFERENCES acc.FiscalYear(FiscalYearID),
        ReferenceNo         NVARCHAR(100)   NULL,
        Narration           NVARCHAR(1000)  NULL,
        TotalDebit          DECIMAL(18,2)   NOT NULL DEFAULT 0,
        TotalCredit         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Draft',
        IsReversed          BIT             NOT NULL DEFAULT 0,
        ReversalVoucherID   INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        IsAutoGenerated     BIT             NOT NULL DEFAULT 0,
        SourceModule        NVARCHAR(50)    NULL,
        PMSProjectID        NVARCHAR(10)    NULL,
        PMSAllotmentID      NVARCHAR(10)    NULL,
        PMSCustomerID       NVARCHAR(10)    NULL,
        PMSDealerID         INT             NULL,
        PMSPaymentID        NVARCHAR(10)    NULL,
        PMSRefundID         NVARCHAR(10)    NULL,
        PMSDealerPaymentID  NVARCHAR(10)    NULL,
        PMSPenaltyID        NVARCHAR(10)    NULL,
        PMSTransferID       NVARCHAR(50)    NULL,
        PMSRentalPaymentID  NVARCHAR(50)    NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        SubmittedBy         NVARCHAR(10)    NULL,
        SubmittedAt         DATETIME2       NULL,
        ApprovedBy          NVARCHAR(10)    NULL,
        ApprovedAt          DATETIME2       NULL,
        PostedBy            NVARCHAR(10)    NULL,
        PostedAt            DATETIME2       NULL,
        ReversedBy          NVARCHAR(10)    NULL,
        ReversedAt          DATETIME2       NULL,
        BankAccountID       INT             NULL
    );
GO

IF OBJECT_ID(N'acc.VoucherLine', N'U') IS NULL
CREATE TABLE acc.VoucherLine (
        VoucherLineID       INT IDENTITY(1,1) PRIMARY KEY,
        VoucherID           INT             NOT NULL
            REFERENCES acc.Voucher(VoucherID),
        LineNumber          SMALLINT        NOT NULL,
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        SubLedgerType       NVARCHAR(30)    NULL,
        SubLedgerID         NVARCHAR(10)    NULL,
        CostCenterID        INT             NULL
            REFERENCES acc.CostCenter(CostCenterID),
        DebitAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        CreditAmount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        FxAmount            DECIMAL(18,2)   NULL,
        ExchangeRate        DECIMAL(18,6)   NULL DEFAULT 1,
        Currency            NVARCHAR(10)    NULL DEFAULT 'PKR',
        Description         NVARCHAR(500)   NULL,
        Reconciled          BIT             NOT NULL DEFAULT 0
    );
GO

/* MODULE E */
IF OBJECT_ID(N'acc.BankAccount', N'U') IS NULL
CREATE TABLE acc.BankAccount (
        BankAccountID       INT IDENTITY(1,1) PRIMARY KEY,
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        BankName            NVARCHAR(150)   NOT NULL,
        BranchName          NVARCHAR(150)   NULL,
        BranchCode          NVARCHAR(20)    NULL,
        AccountTitle        NVARCHAR(200)   NOT NULL,
        AccountNumber       NVARCHAR(50)    NOT NULL,
        IBAN                NVARCHAR(34)    NULL,
        AccountType         NVARCHAR(30)    NULL,
        Currency            NVARCHAR(10)    NOT NULL DEFAULT 'PKR',
        OpeningBalance      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        OpeningDate         DATE            NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

/* FK Voucher.BankAccountID — column created on acc.Voucher above; BankAccount table now exists. */
IF COL_LENGTH(N'acc.Voucher', N'BankAccountID') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Voucher_BankAccount')
BEGIN
    ALTER TABLE acc.Voucher WITH CHECK ADD CONSTRAINT FK_Voucher_BankAccount
        FOREIGN KEY (BankAccountID) REFERENCES acc.BankAccount(BankAccountID);
END
GO

IF OBJECT_ID(N'acc.ChequeBook', N'U') IS NULL
CREATE TABLE acc.ChequeBook (
        ChequeBookID        INT IDENTITY(1,1) PRIMARY KEY,
        BankAccountID       INT             NOT NULL
            REFERENCES acc.BankAccount(BankAccountID),
        SeriesFrom          NVARCHAR(20)    NOT NULL,
        SeriesTo            NVARCHAR(20)    NOT NULL,
        TotalLeaves         INT             NOT NULL,
        UsedLeaves          INT             NOT NULL DEFAULT 0,
        IssuedDate          DATE            NOT NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

IF OBJECT_ID(N'acc.ChequeRegister', N'U') IS NULL
CREATE TABLE acc.ChequeRegister (
        ChequeRegisterID    INT IDENTITY(1,1) PRIMARY KEY,
        BankAccountID       INT             NOT NULL
            REFERENCES acc.BankAccount(BankAccountID),
        ChequeBookID        INT             NULL
            REFERENCES acc.ChequeBook(ChequeBookID),
        ChequeNo            NVARCHAR(30)    NOT NULL,
        ChequeDate          DATE            NOT NULL,
        EntryDate           DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        IsPostDated         BIT             NOT NULL DEFAULT 0,
        ClearanceDate       DATE            NULL,
        ChequeType          NVARCHAR(20)    NOT NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        PayableTo           NVARCHAR(200)   NULL,
        ReceivedFrom        NVARCHAR(200)   NULL,
        Status              NVARCHAR(30)    NOT NULL DEFAULT 'Pending',
        BounceReason        NVARCHAR(300)   NULL,
        BounceVoucherID     INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        BounceChargeAmount  DECIMAL(18,2)   NULL DEFAULT 0,
        ReplacedByChequeID  INT             NULL
            REFERENCES acc.ChequeRegister(ChequeRegisterID),
        VoucherID           INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        SubLedgerType       NVARCHAR(30)    NULL,
        SubLedgerID         NVARCHAR(10)    NULL,
        Remarks             NVARCHAR(500)   NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

IF OBJECT_ID(N'acc.BankReconciliation', N'U') IS NULL
CREATE TABLE acc.BankReconciliation (
        ReconciliationID    INT IDENTITY(1,1) PRIMARY KEY,
        BankAccountID       INT             NOT NULL
            REFERENCES acc.BankAccount(BankAccountID),
        PeriodID            INT             NOT NULL
            REFERENCES acc.AccountingPeriod(PeriodID),
        StatementDate       DATE            NOT NULL,
        BankStatementBalance DECIMAL(18,2)  NOT NULL,
        BookBalance         DECIMAL(18,2)   NOT NULL,
        Difference          AS (BankStatementBalance - BookBalance),
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Draft',
        ReconciledBy        NVARCHAR(10)    NULL,
        ReconciledAt        DATETIME2       NULL,
        Notes               NVARCHAR(1000)  NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

IF OBJECT_ID(N'acc.BankReconciliationLine', N'U') IS NULL
CREATE TABLE acc.BankReconciliationLine (
        ReconLineID         INT IDENTITY(1,1) PRIMARY KEY,
        ReconciliationID    INT             NOT NULL
            REFERENCES acc.BankReconciliation(ReconciliationID),
        VoucherLineID       INT             NULL
            REFERENCES acc.VoucherLine(VoucherLineID),
        ChequeRegisterID    INT             NULL
            REFERENCES acc.ChequeRegister(ChequeRegisterID),
        TransactionDate     DATE            NOT NULL,
        Description         NVARCHAR(300)   NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        IsReconciled        BIT             NOT NULL DEFAULT 0,
        ReconciledAt        DATETIME2       NULL
    );
GO

/* MODULE F */
IF OBJECT_ID(N'acc.ARInvoice', N'U') IS NULL
CREATE TABLE acc.ARInvoice (
        ARInvoiceID         INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNo           NVARCHAR(30)    NOT NULL UNIQUE,
        InvoiceDate         DATE            NOT NULL,
        DueDate             DATE            NOT NULL,
        CustomerID          NVARCHAR(10)    NOT NULL,
        ProjectID           NVARCHAR(10)    NULL,
        AllotmentID         NVARCHAR(10)    NULL,
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        InvoiceType         NVARCHAR(50)    NOT NULL,
        SubTotal            DECIMAL(18,2)   NOT NULL DEFAULT 0,
        TaxAmount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
        DiscountAmount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        TotalAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        PaidAmount          DECIMAL(18,2)   NOT NULL DEFAULT 0,
        BalanceAmount       AS (TotalAmount - PaidAmount),
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Unpaid',
        PMSPaymentScheduleID NVARCHAR(10)   NULL,
        PMSPenaltyID        NVARCHAR(10)    NULL,
        VoucherID           INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        CancellationReason  NVARCHAR(300)   NULL,
        Notes               NVARCHAR(500)   NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        LastModifiedBy      NVARCHAR(10)    NULL,
        LastModifiedAt      DATETIME2       NULL
    );
GO

IF OBJECT_ID(N'acc.ARReceipt', N'U') IS NULL
CREATE TABLE acc.ARReceipt (
        ARReceiptID         INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNo           NVARCHAR(30)    NOT NULL UNIQUE,
        ReceiptDate         DATE            NOT NULL,
        CustomerID          NVARCHAR(10)    NOT NULL,
        ProjectID           NVARCHAR(10)    NULL,
        AllotmentID         NVARCHAR(10)    NULL,
        ReceivedAmount      DECIMAL(18,2)   NOT NULL,
        PaymentMode         NVARCHAR(30)    NOT NULL,
        BankAccountID       INT             NULL
            REFERENCES acc.BankAccount(BankAccountID),
        ChequeRegisterID    INT             NULL
            REFERENCES acc.ChequeRegister(ChequeRegisterID),
        ChequeNo            NVARCHAR(30)    NULL,
        ChequeDate          DATE            NULL,
        BankName            NVARCHAR(150)   NULL,
        IsPostDated         BIT             NOT NULL DEFAULT 0,
        PMSPaymentID        NVARCHAR(10)    NULL,
        VoucherID           INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Active',
        Remarks             NVARCHAR(500)   NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

IF OBJECT_ID(N'acc.ARReceiptAllocation', N'U') IS NULL
CREATE TABLE acc.ARReceiptAllocation (
        AllocationID        INT IDENTITY(1,1) PRIMARY KEY,
        ARReceiptID         INT             NOT NULL
            REFERENCES acc.ARReceipt(ARReceiptID),
        ARInvoiceID         INT             NOT NULL
            REFERENCES acc.ARInvoice(ARInvoiceID),
        AllocatedAmount     DECIMAL(18,2)   NOT NULL,
        AllocatedAt         DATETIME2       NOT NULL DEFAULT GETDATE(),
        AllocatedBy         NVARCHAR(10)    NOT NULL
    );
GO

/* MODULE G */
IF OBJECT_ID(N'acc.Vendor', N'U') IS NULL
CREATE TABLE acc.Vendor (
        VendorID            INT IDENTITY(1,1) PRIMARY KEY,
        VendorCode          NVARCHAR(20)    NOT NULL UNIQUE,
        VendorName          NVARCHAR(200)   NOT NULL,
        VendorType          NVARCHAR(50)    NULL,
        NTN                 NVARCHAR(30)    NULL,
        STRN                NVARCHAR(30)    NULL,
        ContactPerson       NVARCHAR(100)   NULL,
        Phone               NVARCHAR(30)    NULL,
        Email               NVARCHAR(150)   NULL,
        Address             NVARCHAR(500)   NULL,
        BankAccountTitle    NVARCHAR(200)   NULL,
        BankAccountNumber   NVARCHAR(50)    NULL,
        BankName            NVARCHAR(150)   NULL,
        IBAN                NVARCHAR(34)    NULL,
        AccountHeadID       INT             NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedBy           NVARCHAR(10)    NOT NULL
    );
GO

IF OBJECT_ID(N'acc.APBill', N'U') IS NULL
CREATE TABLE acc.APBill (
        APBillID            INT IDENTITY(1,1) PRIMARY KEY,
        BillNo              NVARCHAR(30)    NOT NULL UNIQUE,
        BillDate            DATE            NOT NULL,
        DueDate             DATE            NOT NULL,
        VendorID            INT             NOT NULL
            REFERENCES acc.Vendor(VendorID),
        ProjectID           NVARCHAR(10)    NULL,
        CostCenterID        INT             NULL
            REFERENCES acc.CostCenter(CostCenterID),
        BillType            NVARCHAR(50)    NOT NULL,
        SubTotal            DECIMAL(18,2)   NOT NULL DEFAULT 0,
        WHTAmount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
        GSTAmount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
        OtherTaxAmount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        TotalAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        RetentionAmount     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        RetentionReleased   DECIMAL(18,2)   NOT NULL DEFAULT 0,
        RetentionBalance    AS (RetentionAmount - RetentionReleased),
        PaidAmount          DECIMAL(18,2)   NOT NULL DEFAULT 0,
        BalanceAmount       AS (TotalAmount - RetentionAmount - PaidAmount),
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Unpaid',
        VoucherID           INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        Notes               NVARCHAR(500)   NULL,
        AttachmentPath      NVARCHAR(500)   NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        ApprovedBy          NVARCHAR(10)    NULL,
        ApprovedAt          DATETIME2       NULL
    );
GO

IF OBJECT_ID(N'acc.APBillLine', N'U') IS NULL
CREATE TABLE acc.APBillLine (
        APBillLineID        INT IDENTITY(1,1) PRIMARY KEY,
        APBillID            INT             NOT NULL
            REFERENCES acc.APBill(APBillID),
        LineNumber          SMALLINT        NOT NULL,
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        Description         NVARCHAR(300)   NOT NULL,
        Quantity            DECIMAL(10,3)   NULL,
        UnitPrice           DECIMAL(18,2)   NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        WHTRate             DECIMAL(5,2)    NULL DEFAULT 0,
        WHTAmount           DECIMAL(18,2)   NULL DEFAULT 0,
        GSTRate             DECIMAL(5,2)    NULL DEFAULT 0,
        GSTAmount           DECIMAL(18,2)   NULL DEFAULT 0
    );
GO

IF OBJECT_ID(N'acc.APPayment', N'U') IS NULL
CREATE TABLE acc.APPayment (
        APPaymentID         INT IDENTITY(1,1) PRIMARY KEY,
        PaymentNo           NVARCHAR(30)    NOT NULL UNIQUE,
        PaymentDate         DATE            NOT NULL,
        VendorID            INT             NOT NULL
            REFERENCES acc.Vendor(VendorID),
        PaidAmount          DECIMAL(18,2)   NOT NULL,
        PaymentMode         NVARCHAR(30)    NOT NULL,
        BankAccountID       INT             NULL
            REFERENCES acc.BankAccount(BankAccountID),
        ChequeRegisterID    INT             NULL
            REFERENCES acc.ChequeRegister(ChequeRegisterID),
        ChequeNo            NVARCHAR(30)    NULL,
        ChequeDate          DATE            NULL,
        VoucherID           INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Posted',
        Remarks             NVARCHAR(500)   NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

IF OBJECT_ID(N'acc.APPaymentAllocation', N'U') IS NULL
CREATE TABLE acc.APPaymentAllocation (
        AllocationID        INT IDENTITY(1,1) PRIMARY KEY,
        APPaymentID         INT             NOT NULL
            REFERENCES acc.APPayment(APPaymentID),
        APBillID            INT             NOT NULL
            REFERENCES acc.APBill(APBillID),
        AllocatedAmount     DECIMAL(18,2)   NOT NULL,
        IsRetentionRelease  BIT             NOT NULL DEFAULT 0,
        AllocatedAt         DATETIME2       NOT NULL DEFAULT GETDATE(),
        AllocatedBy         NVARCHAR(10)    NOT NULL
    );
GO

/* MODULE H */
IF OBJECT_ID(N'acc.TaxType', N'U') IS NULL
CREATE TABLE acc.TaxType (
        TaxTypeID           INT IDENTITY(1,1) PRIMARY KEY,
        TaxCode             NVARCHAR(20)    NOT NULL UNIQUE,
        TaxName             NVARCHAR(100)   NOT NULL,
        TaxCategory         NVARCHAR(30)    NOT NULL,
        AppliesTo           NVARCHAR(30)    NOT NULL,
        Rate                DECIMAL(7,4)    NOT NULL DEFAULT 0,
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        IsActive            BIT             NOT NULL DEFAULT 1,
        EffectiveFrom       DATE            NULL,
        EffectiveTo         DATE            NULL
    );
GO

IF OBJECT_ID(N'acc.TaxTransaction', N'U') IS NULL
CREATE TABLE acc.TaxTransaction (
        TaxTransactionID    INT IDENTITY(1,1) PRIMARY KEY,
        VoucherID           INT             NOT NULL
            REFERENCES acc.Voucher(VoucherID),
        TaxTypeID           INT             NOT NULL
            REFERENCES acc.TaxType(TaxTypeID),
        TaxableAmount       DECIMAL(18,2)   NOT NULL,
        TaxRate             DECIMAL(7,4)    NOT NULL,
        TaxAmount           DECIMAL(18,2)   NOT NULL,
        SubLedgerType       NVARCHAR(30)    NULL,
        SubLedgerID         NVARCHAR(10)    NULL,
        PeriodID            INT             NOT NULL
            REFERENCES acc.AccountingPeriod(PeriodID),
        ChallanNo           NVARCHAR(50)    NULL,
        DepositedDate       DATE            NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

IF OBJECT_ID(N'acc.WHTCertificate', N'U') IS NULL
CREATE TABLE acc.WHTCertificate (
        WHTCertificateID    INT IDENTITY(1,1) PRIMARY KEY,
        CertificateNo       NVARCHAR(50)    NOT NULL UNIQUE,
        IssueDate           DATE            NOT NULL,
        PeriodID            INT             NOT NULL
            REFERENCES acc.AccountingPeriod(PeriodID),
        SubLedgerType       NVARCHAR(30)    NOT NULL,
        SubLedgerID         NVARCHAR(10)    NOT NULL,
        TotalTaxableAmount  DECIMAL(18,2)   NOT NULL,
        TotalWHTAmount      DECIMAL(18,2)   NOT NULL,
        TaxTypeID           INT             NOT NULL
            REFERENCES acc.TaxType(TaxTypeID),
        ChallanNo           NVARCHAR(50)    NULL,
        IssuedBy            NVARCHAR(10)    NOT NULL,
        Notes               NVARCHAR(500)   NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

/* MODULE I */
IF OBJECT_ID(N'acc.DealerCommissionVoucher', N'U') IS NULL
CREATE TABLE acc.DealerCommissionVoucher (
        CommissionVoucherID INT IDENTITY(1,1) PRIMARY KEY,
        VoucherNo           NVARCHAR(30)    NOT NULL UNIQUE,
        VoucherDate         DATE            NOT NULL,
        DealerID            INT             NOT NULL,
        ProjectID           NVARCHAR(10)    NULL,
        AllotmentID         NVARCHAR(10)    NULL,
        PMSDealerPaymentID  NVARCHAR(10)    NULL,
        GrossCommission     DECIMAL(18,2)   NOT NULL,
        WHTRate             DECIMAL(5,2)    NOT NULL DEFAULT 0,
        WHTAmount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
        NetPayable          DECIMAL(18,2)   NOT NULL,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
        AccountingVoucherID INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        APPaymentID         INT             NULL
            REFERENCES acc.APPayment(APPaymentID),
        ApprovedBy          NVARCHAR(10)    NULL,
        ApprovedAt          DATETIME2       NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

/* MODULE J */
IF OBJECT_ID(N'acc.RefundVoucher', N'U') IS NULL
CREATE TABLE acc.RefundVoucher (
        RefundVoucherID     INT IDENTITY(1,1) PRIMARY KEY,
        VoucherNo           NVARCHAR(30)    NOT NULL UNIQUE,
        VoucherDate         DATE            NOT NULL,
        CustomerID          NVARCHAR(10)    NOT NULL,
        AllotmentID         NVARCHAR(10)    NULL,
        PMSRefundID         NVARCHAR(10)    NULL,
        GrossRefundAmount   DECIMAL(18,2)   NOT NULL,
        ProcessingFee       DECIMAL(18,2)   NOT NULL DEFAULT 0,
        PenaltyDeduction    DECIMAL(18,2)   NOT NULL DEFAULT 0,
        OtherDeduction      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        NetRefundAmount     DECIMAL(18,2)   NOT NULL,
        PaymentMode         NVARCHAR(30)    NOT NULL,
        BankAccountID       INT             NULL
            REFERENCES acc.BankAccount(BankAccountID),
        ChequeRegisterID    INT             NULL
            REFERENCES acc.ChequeRegister(ChequeRegisterID),
        ChequeNo            NVARCHAR(30)    NULL,
        ChequeDate          DATE            NULL,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
        AccountingVoucherID INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        ApprovedBy          NVARCHAR(10)    NULL,
        ApprovedAt          DATETIME2       NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

/* MODULE K */
IF OBJECT_ID(N'acc.Budget', N'U') IS NULL
CREATE TABLE acc.Budget (
        BudgetID            INT IDENTITY(1,1) PRIMARY KEY,
        BudgetName          NVARCHAR(150)   NOT NULL,
        FiscalYearID        INT             NOT NULL
            REFERENCES acc.FiscalYear(FiscalYearID),
        BudgetType          NVARCHAR(30)    NOT NULL DEFAULT 'Annual',
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Draft',
        ApprovedBy          NVARCHAR(10)    NULL,
        ApprovedAt          DATETIME2       NULL,
        Notes               NVARCHAR(500)   NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

IF OBJECT_ID(N'acc.BudgetLine', N'U') IS NULL
CREATE TABLE acc.BudgetLine (
        BudgetLineID        INT IDENTITY(1,1) PRIMARY KEY,
        BudgetID            INT             NOT NULL
            REFERENCES acc.Budget(BudgetID),
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        CostCenterID        INT             NULL
            REFERENCES acc.CostCenter(CostCenterID),
        PeriodID            INT             NULL
            REFERENCES acc.AccountingPeriod(PeriodID),
        BudgetedAmount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        RevisedAmount       DECIMAL(18,2)   NULL,
        Remarks             NVARCHAR(300)   NULL
    );
GO

/* MODULE L */
IF OBJECT_ID(N'acc.OpeningBalance', N'U') IS NULL
CREATE TABLE acc.OpeningBalance (
        OpeningBalanceID    INT IDENTITY(1,1) PRIMARY KEY,
        FiscalYearID        INT             NOT NULL
            REFERENCES acc.FiscalYear(FiscalYearID),
        AccountHeadID       INT             NOT NULL
            REFERENCES acc.AccountHead(AccountHeadID),
        SubLedgerType       NVARCHAR(30)    NULL,
        SubLedgerID         NVARCHAR(10)    NULL,
        DebitAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        CreditAmount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        IsPosted            BIT             NOT NULL DEFAULT 0,
        PostedVoucherID     INT             NULL
            REFERENCES acc.Voucher(VoucherID),
        Notes               NVARCHAR(300)   NULL,
        CreatedBy           NVARCHAR(10)    NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
GO

/* MODULE M */
IF OBJECT_ID(N'acc.AccountingAuditLog', N'U') IS NULL
CREATE TABLE acc.AccountingAuditLog (
        LogID               BIGINT IDENTITY(1,1) PRIMARY KEY,
        TableName           NVARCHAR(100)   NOT NULL,
        RecordID            NVARCHAR(50)    NOT NULL,
        Action              NVARCHAR(20)    NOT NULL,
        OldValues           NVARCHAR(MAX)   NULL,
        NewValues           NVARCHAR(MAX)   NULL,
        ChangedBy           NVARCHAR(10)    NOT NULL,
        ChangedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
        IPAddress           NVARCHAR(50)    NULL,
        UserAgent           NVARCHAR(300)   NULL
    );
GO

IF NOT EXISTS (SELECT 1 FROM acc.VoucherType)
BEGIN
    INSERT INTO acc.VoucherType (TypeCode, TypeName, Prefix, IsAutoNumbered, IsActive) VALUES
    ('JV',  'Journal Voucher',         'JV-',   1, 1),
    ('BPV', 'Bank Payment Voucher',    'BPV-',  1, 1),
    ('BRV', 'Bank Receipt Voucher',    'BRV-',  1, 1),
    ('CPV', 'Cash Payment Voucher',    'CPV-',  1, 1),
    ('CRV', 'Cash Receipt Voucher',    'CRV-',  1, 1),
    ('PDC', 'Post-Dated Cheque',       'PDC-',  1, 1),
    ('RV',  'Reversal Voucher',        'RV-',   1, 1),
    ('OBV', 'Opening Balance Voucher', 'OBV-',  1, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM acc.AccountCategory)
BEGIN
    INSERT INTO acc.AccountCategory (CategoryName, NatureType) VALUES
    ('Asset',     'Debit'),
    ('Liability', 'Credit'),
    ('Equity',    'Credit'),
    ('Revenue',   'Credit'),
    ('Expense',   'Debit');
END
GO

/* §4 — placeholder liability ledgers for TaxType.AccountHeadID (replace with real COA in production) */
IF NOT EXISTS (SELECT 1 FROM acc.AccountHead WHERE AccountCode = N'2160-WHT')
BEGIN
    DECLARE @LiabilityCat INT = (SELECT TOP 1 AccountCategoryID FROM acc.AccountCategory WHERE CategoryName = N'Liability');
    IF @LiabilityCat IS NOT NULL
        INSERT INTO acc.AccountHead (
            AccountCategoryID, AccountCode, AccountName, AccountLevel,
            IsControlAccount, AllowDirectPosting, OpeningBalance, IsActive, CreatedBy)
        VALUES
            (@LiabilityCat, N'2160-WHT', N'Withholding tax payable (AMS seed)', 3, 0, 1, 0, 1, N'SYSTEM'),
            (@LiabilityCat, N'2170-GST', N'GST payable (AMS seed)', 3, 0, 1, 0, 1, N'SYSTEM');
END
GO

IF NOT EXISTS (SELECT 1 FROM acc.TaxType)
BEGIN
    DECLARE @WHTHead INT = (SELECT AccountHeadID FROM acc.AccountHead WHERE AccountCode = N'2160-WHT');
    DECLARE @GSTHead INT = (SELECT AccountHeadID FROM acc.AccountHead WHERE AccountCode = N'2170-GST');
    IF @WHTHead IS NOT NULL AND @GSTHead IS NOT NULL
        INSERT INTO acc.TaxType (TaxCode, TaxName, TaxCategory, AppliesTo, Rate, AccountHeadID, IsActive) VALUES
        (N'WHT-RENT',  N'WHT on Rent (Section 155)',       N'WHT', N'Payable', 15.0000, @WHTHead, 1),
        (N'WHT-SERV',  N'WHT on Services (Section 153)',   N'WHT', N'Payable',  8.0000, @WHTHead, 1),
        (N'WHT-CONTR', N'WHT on Contracts (Section 153)',  N'WHT', N'Payable',  7.0000, @WHTHead, 1),
        (N'WHT-COMM',  N'WHT on Commission (Section 233)', N'WHT', N'Payable', 10.0000, @WHTHead, 1),
        (N'GST-17',    N'General Sales Tax 17%',           N'GST', N'Both',    17.0000, @GSTHead, 1),
        (N'GST-13',    N'Reduced GST 13%',                 N'GST', N'Both',    13.0000, @GSTHead, 1);
END
GO

PRINT N'AMS acc schema + tables + seeds ready (VoucherType, AccountCategory, TaxType placeholders).';
GO
