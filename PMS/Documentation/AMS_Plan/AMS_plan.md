# Accounting Management System (AMS) — Development Plan
> **Context file for Cursor AI**
> This document is the single source of truth for building the Accounting Management System
> as a module inside the existing Property Management System (PMS) called **PMSAbbas**.

**Execution order (one step at a time):** see [`STEP_SEQUENCE.md`](STEP_SEQUENCE.md) and per-step files under [`steps/`](steps/).

---

## 1. Project Overview

| Field | Detail |
|---|---|
| System | Accounting Management System (AMS) |
| Type | Integrated module inside existing PMS |
| Database | MS SQL Server — same database: `PMSAbbas` |
| Schema separation | AMS tables live in `acc` schema; PMS stays in `dbo` |
| Currency | PKR (primary); design must support multi-currency extension |
| Compliance | Pakistan FBR — WHT, GST, income tax |
| Users | Accounts department staff, Finance Manager, CEO (reports only) |

### Core principle
AMS is **not** a standalone system. It shares the same SQL Server database as PMS.
PMS tables in `dbo` schema are **read-only** from AMS perspective — AMS only writes to `acc.*` tables.
PMS events (payments, refunds, penalties, dealer payments) trigger AMS entries via application-layer service calls or database triggers — never direct writes from PMS into AMS tables by PMS code.

---

## 2. Existing PMS Database Tables (dbo schema — READ ONLY from AMS)

These are the PMS tables AMS will reference via foreign keys. Do NOT modify their structure.

```
dbo.Projects               — master project list
dbo.ProjectSubProjects     — phases / blocks within a project
dbo.Customers              — buyer master
dbo.JointOwner             — joint ownership records
dbo.Dealers                — dealer/agent master
dbo.Allotments             — plot/unit allotment to customer
dbo.Allotment              — (legacy, check for duplicates)
dbo.Payments               — installment payments received in PMS
dbo.PaymentSchedule        — scheduled installment plan per allotment
dbo.PaymentPlan            — payment plan template
dbo.DealerPayments         — dealer commission payments in PMS
dbo.Refunds                — customer refund requests
dbo.RefundCheques          — cheques issued for refunds
dbo.Penalties              — penalty records
dbo.Waiver                 — penalty waivers
dbo.TransferFee            — property transfer fee records
dbo.Transfers              — property transfer records
dbo.Rental                 — rental agreement records
dbo.RentalPayments         — rental payment records
dbo.Registration           — property registration records
dbo.NDC                    — No Demand Certificate records
dbo.NDCs                   — (check for duplicate, may be same as NDC)
dbo.Possession             — possession handover records
dbo.Property               — property/plot master
dbo.Users                  — system users (shared with AMS for CreatedBy/ApprovedBy)
dbo.UserModulePermission   — module-level permissions (extend for AMS modules)
dbo.Configuration          — system config key-value store
dbo.ActivityLog            — existing audit log (AMS has its own: acc.AccountingAuditLog)
```

---

## 3. AMS Database Schema (acc schema — AMS OWNS these)

### 3.1 Create Schema
```sql
CREATE SCHEMA acc;
```

### 3.2 Full Table Definitions

```sql
-- ============================================================
-- MODULE A: CHART OF ACCOUNTS
-- ============================================================

CREATE TABLE acc.AccountCategory (
    AccountCategoryID   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName        NVARCHAR(100)   NOT NULL,
    -- Asset | Liability | Equity | Revenue | Expense
    NatureType          NVARCHAR(20)    NOT NULL,
    -- 'Debit' | 'Credit'
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

CREATE TABLE acc.AccountHead (
    AccountHeadID       INT IDENTITY(1,1) PRIMARY KEY,
    AccountCategoryID   INT             NOT NULL
        REFERENCES acc.AccountCategory(AccountCategoryID),
    ParentAccountHeadID INT             NULL
        REFERENCES acc.AccountHead(AccountHeadID),
    AccountCode         NVARCHAR(30)    NOT NULL UNIQUE,
    -- e.g. 1000, 1001, 1001-01
    AccountName         NVARCHAR(150)   NOT NULL,
    AccountLevel        TINYINT         NOT NULL DEFAULT 1,
    -- 1=Group, 2=Sub-Group, 3=Ledger
    IsControlAccount    BIT             NOT NULL DEFAULT 0,
    AllowDirectPosting  BIT             NOT NULL DEFAULT 1,
    -- false for group-level heads
    OpeningBalance      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    OpeningBalanceDate  DATE            NULL,
    OpeningBalanceType  NVARCHAR(10)    NULL,
    -- 'Debit' | 'Credit'
    Description         NVARCHAR(500)   NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
    -- References dbo.Users
);

-- ============================================================
-- MODULE B: FISCAL YEAR & PERIODS
-- ============================================================

CREATE TABLE acc.FiscalYear (
    FiscalYearID        INT IDENTITY(1,1) PRIMARY KEY,
    YearName            NVARCHAR(50)    NOT NULL,   -- e.g. FY 2024-25
    StartDate           DATE            NOT NULL,
    EndDate             DATE            NOT NULL,
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Open',
    -- Open | Closed | Locked
    ClosedBy            INT             NULL,
    ClosedAt            DATETIME2       NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
);

CREATE TABLE acc.AccountingPeriod (
    PeriodID            INT IDENTITY(1,1) PRIMARY KEY,
    FiscalYearID        INT             NOT NULL
        REFERENCES acc.FiscalYear(FiscalYearID),
    PeriodName          NVARCHAR(50)    NOT NULL,   -- e.g. July 2024
    StartDate           DATE            NOT NULL,
    EndDate             DATE            NOT NULL,
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Open',
    -- Open | Closed
    ClosedBy            INT             NULL,
    ClosedAt            DATETIME2       NULL
);

-- ============================================================
-- MODULE C: COST CENTERS (PROJECT / PHASE / DEPARTMENT)
-- ============================================================

CREATE TABLE acc.CostCenter (
    CostCenterID        INT IDENTITY(1,1) PRIMARY KEY,
    CostCenterCode      NVARCHAR(20)    NOT NULL UNIQUE,
    CostCenterName      NVARCHAR(150)   NOT NULL,
    ParentCostCenterID  INT             NULL
        REFERENCES acc.CostCenter(CostCenterID),
    ProjectID           INT             NULL,
    -- FK → dbo.Projects.ProjectID
    SubProjectID        INT             NULL,
    -- FK → dbo.ProjectSubProjects.SubProjectID
    CostCenterType      NVARCHAR(30)    NULL,
    -- Project | Phase | Block | Department | Company
    BudgetAmount        DECIMAL(18,2)   NULL DEFAULT 0,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
);

-- ============================================================
-- MODULE D: VOUCHER ENGINE (CORE DOUBLE-ENTRY)
-- ============================================================

CREATE TABLE acc.VoucherType (
    VoucherTypeID       INT IDENTITY(1,1) PRIMARY KEY,
    TypeCode            NVARCHAR(10)    NOT NULL UNIQUE,
    -- JV | BPV | BRV | CPV | CRV | SV | PV | INV | RV
    TypeName            NVARCHAR(100)   NOT NULL,
    -- Journal Voucher | Bank Payment Voucher | Bank Receipt Voucher
    -- Cash Payment Voucher | Cash Receipt Voucher | Sales Voucher
    -- Purchase Voucher | Invoice | Reversal Voucher
    Prefix              NVARCHAR(10)    NULL,
    IsAutoNumbered      BIT             NOT NULL DEFAULT 1,
    IsActive            BIT             NOT NULL DEFAULT 1
);

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
    -- cheque no, receipt no, external ref
    Narration           NVARCHAR(1000)  NULL,
    TotalDebit          DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TotalCredit         DECIMAL(18,2)   NOT NULL DEFAULT 0,
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Draft',
    -- Draft | Pending Approval | Approved | Posted | Reversed | Cancelled
    IsReversed          BIT             NOT NULL DEFAULT 0,
    ReversalVoucherID   INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    IsAutoGenerated     BIT             NOT NULL DEFAULT 0,
    -- true when triggered by PMS event
    SourceModule        NVARCHAR(50)    NULL,
    -- 'PMS_Payment' | 'PMS_Refund' | 'PMS_Dealer' | 'PMS_Penalty' | 'Manual'
    -- PMS reference links (nullable — only set when triggered by PMS)
    PMSProjectID        INT             NULL,
    -- FK → dbo.Projects
    PMSAllotmentID      INT             NULL,
    -- FK → dbo.Allotments
    PMSCustomerID       INT             NULL,
    -- FK → dbo.Customers
    PMSDealerID         INT             NULL,
    -- FK → dbo.Dealers
    PMSPaymentID        INT             NULL,
    -- FK → dbo.Payments
    PMSRefundID         INT             NULL,
    -- FK → dbo.Refunds
    PMSDealerPaymentID  INT             NULL,
    -- FK → dbo.DealerPayments
    PMSPenaltyID        INT             NULL,
    -- FK → dbo.Penalties
    PMSTransferID       INT             NULL,
    -- FK → dbo.Transfers
    PMSRentalPaymentID  INT             NULL,
    -- FK → dbo.RentalPayments
    -- Audit
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    SubmittedBy         INT             NULL,
    SubmittedAt         DATETIME2       NULL,
    ApprovedBy          INT             NULL,
    ApprovedAt          DATETIME2       NULL,
    PostedBy            INT             NULL,
    PostedAt            DATETIME2       NULL,
    ReversedBy          INT             NULL,
    ReversedAt          DATETIME2       NULL
);

CREATE TABLE acc.VoucherLine (
    VoucherLineID       INT IDENTITY(1,1) PRIMARY KEY,
    VoucherID           INT             NOT NULL
        REFERENCES acc.Voucher(VoucherID),
    LineNo              SMALLINT        NOT NULL,
    AccountHeadID       INT             NOT NULL
        REFERENCES acc.AccountHead(AccountHeadID),
    SubLedgerType       NVARCHAR(30)    NULL,
    -- 'Customer' | 'Dealer' | 'Employee' | 'Vendor'
    SubLedgerID         INT             NULL,
    -- ID in respective PMS or AMS table depending on SubLedgerType
    CostCenterID        INT             NULL
        REFERENCES acc.CostCenter(CostCenterID),
    DebitAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
    CreditAmount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
    ForeignAmount       DECIMAL(18,2)   NULL,
    -- for future multi-currency
    ExchangeRate        DECIMAL(18,6)   NULL DEFAULT 1,
    Currency            NVARCHAR(10)    NULL DEFAULT 'PKR',
    Description         NVARCHAR(500)   NULL,
    Reconciled          BIT             NOT NULL DEFAULT 0
);

-- ============================================================
-- MODULE E: BANK & CASH MANAGEMENT
-- ============================================================

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
    -- Current | Savings | PLS | Foreign Currency
    Currency            NVARCHAR(10)    NOT NULL DEFAULT 'PKR',
    OpeningBalance      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    OpeningDate         DATE            NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
);

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

CREATE TABLE acc.ChequeRegister (
    ChequeRegisterID    INT IDENTITY(1,1) PRIMARY KEY,
    BankAccountID       INT             NOT NULL
        REFERENCES acc.BankAccount(BankAccountID),
    ChequeBookID        INT             NULL
        REFERENCES acc.ChequeBook(ChequeBookID),
    ChequeNo            NVARCHAR(30)    NOT NULL,
    ChequeDate          DATE            NOT NULL,
    -- actual date on the cheque (may be post-dated)
    EntryDate           DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    IsPostDated         BIT             NOT NULL DEFAULT 0,
    ClearanceDate       DATE            NULL,
    -- date cheque actually cleared
    ChequeType          NVARCHAR(20)    NOT NULL,
    -- 'Issued' (outgoing) | 'Received' (incoming)
    Amount              DECIMAL(18,2)   NOT NULL,
    PayableTo           NVARCHAR(200)   NULL,
    ReceivedFrom        NVARCHAR(200)   NULL,
    Status              NVARCHAR(30)    NOT NULL DEFAULT 'Pending',
    -- Pending | Cleared | Bounced | Cancelled | Stopped | Replaced
    BounceReason        NVARCHAR(300)   NULL,
    BounceVoucherID     INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    -- reversal JV on bounce
    BounceChargeAmount  DECIMAL(18,2)   NULL DEFAULT 0,
    ReplacedByChequeID  INT             NULL
        REFERENCES acc.ChequeRegister(ChequeRegisterID),
    VoucherID           INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    SubLedgerType       NVARCHAR(30)    NULL,
    SubLedgerID         INT             NULL,
    -- PMSCustomerID or VendorID depending on direction
    Remarks             NVARCHAR(500)   NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
);

-- Post-Dated Cheque (PDC) tracking view helper flag is on ChequeRegister.IsPostDated
-- A scheduled job queries: WHERE IsPostDated=1 AND Status='Pending' AND ChequeDate <= GETDATE()
-- and moves them to cleared/presented status

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
    -- Draft | Finalized
    ReconciledBy        INT             NULL,
    ReconciledAt        DATETIME2       NULL,
    Notes               NVARCHAR(1000)  NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
);

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

-- ============================================================
-- MODULE F: ACCOUNTS RECEIVABLE (AR)
-- ============================================================

CREATE TABLE acc.ARInvoice (
    ARInvoiceID         INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNo           NVARCHAR(30)    NOT NULL UNIQUE,
    InvoiceDate         DATE            NOT NULL,
    DueDate             DATE            NOT NULL,
    CustomerID          INT             NOT NULL,
    -- FK → dbo.Customers
    ProjectID           INT             NULL,
    -- FK → dbo.Projects
    AllotmentID         INT             NULL,
    -- FK → dbo.Allotments
    AccountHeadID       INT             NOT NULL
        REFERENCES acc.AccountHead(AccountHeadID),
    -- AR Control Account
    InvoiceType         NVARCHAR(50)    NOT NULL,
    -- InstallmentDue | BookingPayment | Penalty | RegistrationFee
    -- NDCFee | TransferFee | RentalDue | PossessionCharges | Misc
    SubTotal            DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TaxAmount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
    DiscountAmount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TotalAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
    PaidAmount          DECIMAL(18,2)   NOT NULL DEFAULT 0,
    BalanceAmount       AS (TotalAmount - PaidAmount),
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Unpaid',
    -- Unpaid | PartiallyPaid | Paid | Cancelled | WrittenOff
    PMSPaymentScheduleID INT            NULL,
    -- FK → dbo.PaymentSchedule (auto-link when generated from schedule)
    PMSPenaltyID        INT             NULL,
    -- FK → dbo.Penalties
    VoucherID           INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    CancellationReason  NVARCHAR(300)   NULL,
    Notes               NVARCHAR(500)   NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    LastModifiedBy      INT             NULL,
    LastModifiedAt      DATETIME2       NULL
);

CREATE TABLE acc.ARReceipt (
    ARReceiptID         INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptNo           NVARCHAR(30)    NOT NULL UNIQUE,
    ReceiptDate         DATE            NOT NULL,
    CustomerID          INT             NOT NULL,
    -- FK → dbo.Customers
    ProjectID           INT             NULL,
    AllotmentID         INT             NULL,
    ReceivedAmount      DECIMAL(18,2)   NOT NULL,
    PaymentMode         NVARCHAR(30)    NOT NULL,
    -- Cash | Cheque | NEFT | RTGS | Online | PDC
    BankAccountID       INT             NULL
        REFERENCES acc.BankAccount(BankAccountID),
    ChequeRegisterID    INT             NULL
        REFERENCES acc.ChequeRegister(ChequeRegisterID),
    ChequeNo            NVARCHAR(30)    NULL,
    ChequeDate          DATE            NULL,
    BankName            NVARCHAR(150)   NULL,
    IsPostDated         BIT             NOT NULL DEFAULT 0,
    PMSPaymentID        INT             NULL,
    -- FK → dbo.Payments
    VoucherID           INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Active',
    -- Active | Reversed | BounceReversed
    Remarks             NVARCHAR(500)   NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

CREATE TABLE acc.ARReceiptAllocation (
    AllocationID        INT IDENTITY(1,1) PRIMARY KEY,
    ARReceiptID         INT             NOT NULL
        REFERENCES acc.ARReceipt(ARReceiptID),
    ARInvoiceID         INT             NOT NULL
        REFERENCES acc.ARInvoice(ARInvoiceID),
    AllocatedAmount     DECIMAL(18,2)   NOT NULL,
    AllocatedAt         DATETIME2       NOT NULL DEFAULT GETDATE(),
    AllocatedBy         INT             NOT NULL
);

-- ============================================================
-- MODULE G: ACCOUNTS PAYABLE (AP)
-- ============================================================

CREATE TABLE acc.Vendor (
    VendorID            INT IDENTITY(1,1) PRIMARY KEY,
    VendorCode          NVARCHAR(20)    NOT NULL UNIQUE,
    VendorName          NVARCHAR(200)   NOT NULL,
    VendorType          NVARCHAR(50)    NULL,
    -- Contractor | Supplier | Utility | Government | Consultant
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
    -- AP sub-ledger account for this vendor
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT             NOT NULL
);

CREATE TABLE acc.APBill (
    APBillID            INT IDENTITY(1,1) PRIMARY KEY,
    BillNo              NVARCHAR(30)    NOT NULL UNIQUE,
    BillDate            DATE            NOT NULL,
    DueDate             DATE            NOT NULL,
    VendorID            INT             NOT NULL
        REFERENCES acc.Vendor(VendorID),
    ProjectID           INT             NULL,
    CostCenterID        INT             NULL
        REFERENCES acc.CostCenter(CostCenterID),
    BillType            NVARCHAR(50)    NOT NULL,
    -- Utility | Procurement | Construction | Maintenance
    -- Professional | Government | Salaries | Misc
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
    -- Unpaid | PartiallyPaid | Paid | Cancelled | Disputed
    VoucherID           INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    Notes               NVARCHAR(500)   NULL,
    AttachmentPath      NVARCHAR(500)   NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    ApprovedBy          INT             NULL,
    ApprovedAt          DATETIME2       NULL
);

CREATE TABLE acc.APBillLine (
    APBillLineID        INT IDENTITY(1,1) PRIMARY KEY,
    APBillID            INT             NOT NULL
        REFERENCES acc.APBill(APBillID),
    LineNo              SMALLINT        NOT NULL,
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

CREATE TABLE acc.APPayment (
    APPaymentID         INT IDENTITY(1,1) PRIMARY KEY,
    PaymentNo           NVARCHAR(30)    NOT NULL UNIQUE,
    PaymentDate         DATE            NOT NULL,
    VendorID            INT             NOT NULL
        REFERENCES acc.Vendor(VendorID),
    PaidAmount          DECIMAL(18,2)   NOT NULL,
    PaymentMode         NVARCHAR(30)    NOT NULL,
    -- Cash | Cheque | NEFT | RTGS | Online
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
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

CREATE TABLE acc.APPaymentAllocation (
    AllocationID        INT IDENTITY(1,1) PRIMARY KEY,
    APPaymentID         INT             NOT NULL
        REFERENCES acc.APPayment(APPaymentID),
    APBillID            INT             NOT NULL
        REFERENCES acc.APBill(APBillID),
    AllocatedAmount     DECIMAL(18,2)   NOT NULL,
    IsRetentionRelease  BIT             NOT NULL DEFAULT 0,
    AllocatedAt         DATETIME2       NOT NULL DEFAULT GETDATE(),
    AllocatedBy         INT             NOT NULL
);

-- ============================================================
-- MODULE H: TAX MANAGEMENT (WHT / GST / FBR)
-- ============================================================

CREATE TABLE acc.TaxType (
    TaxTypeID           INT IDENTITY(1,1) PRIMARY KEY,
    TaxCode             NVARCHAR(20)    NOT NULL UNIQUE,
    -- WHT-RENT | WHT-SERV | WHT-CONTR | GST-17 | GST-13 | FED-16
    TaxName             NVARCHAR(100)   NOT NULL,
    TaxCategory         NVARCHAR(30)    NOT NULL,
    -- WHT | GST | FED | Other
    AppliesTo           NVARCHAR(30)    NOT NULL,
    -- 'Payable' | 'Receivable' | 'Both'
    Rate                DECIMAL(7,4)    NOT NULL DEFAULT 0,
    AccountHeadID       INT             NOT NULL
        REFERENCES acc.AccountHead(AccountHeadID),
    IsActive            BIT             NOT NULL DEFAULT 1,
    EffectiveFrom       DATE            NULL,
    EffectiveTo         DATE            NULL
);

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
    -- 'Customer' | 'Vendor' | 'Dealer'
    SubLedgerID         INT             NULL,
    PeriodID            INT             NOT NULL
        REFERENCES acc.AccountingPeriod(PeriodID),
    ChallanNo           NVARCHAR(50)    NULL,
    -- FBR challan number when deposited
    DepositedDate       DATE            NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

CREATE TABLE acc.WHTCertificate (
    WHTCertificateID    INT IDENTITY(1,1) PRIMARY KEY,
    CertificateNo       NVARCHAR(50)    NOT NULL UNIQUE,
    IssueDate           DATE            NOT NULL,
    PeriodID            INT             NOT NULL
        REFERENCES acc.AccountingPeriod(PeriodID),
    SubLedgerType       NVARCHAR(30)    NOT NULL,
    -- 'Vendor' | 'Dealer'
    SubLedgerID         INT             NOT NULL,
    TotalTaxableAmount  DECIMAL(18,2)   NOT NULL,
    TotalWHTAmount      DECIMAL(18,2)   NOT NULL,
    TaxTypeID           INT             NOT NULL
        REFERENCES acc.TaxType(TaxTypeID),
    ChallanNo           NVARCHAR(50)    NULL,
    IssuedBy            INT             NOT NULL,
    Notes               NVARCHAR(500)   NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- MODULE I: DEALER COMMISSION ACCOUNTING
-- ============================================================

CREATE TABLE acc.DealerCommissionVoucher (
    CommissionVoucherID INT IDENTITY(1,1) PRIMARY KEY,
    VoucherNo           NVARCHAR(30)    NOT NULL UNIQUE,
    VoucherDate         DATE            NOT NULL,
    DealerID            INT             NOT NULL,
    -- FK → dbo.Dealers
    ProjectID           INT             NULL,
    AllotmentID         INT             NULL,
    PMSDealerPaymentID  INT             NULL,
    -- FK → dbo.DealerPayments
    GrossCommission     DECIMAL(18,2)   NOT NULL,
    WHTRate             DECIMAL(5,2)    NOT NULL DEFAULT 0,
    WHTAmount           DECIMAL(18,2)   NOT NULL DEFAULT 0,
    NetPayable          DECIMAL(18,2)   NOT NULL,
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
    -- Pending | Approved | Paid | Cancelled
    AccountingVoucherID INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    APPaymentID         INT             NULL
        REFERENCES acc.APPayment(APPaymentID),
    ApprovedBy          INT             NULL,
    ApprovedAt          DATETIME2       NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- MODULE J: REFUND ACCOUNTING
-- ============================================================

CREATE TABLE acc.RefundVoucher (
    RefundVoucherID     INT IDENTITY(1,1) PRIMARY KEY,
    VoucherNo           NVARCHAR(30)    NOT NULL UNIQUE,
    VoucherDate         DATE            NOT NULL,
    CustomerID          INT             NOT NULL,
    -- FK → dbo.Customers
    AllotmentID         INT             NULL,
    PMSRefundID         INT             NULL,
    -- FK → dbo.Refunds
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
    -- Pending | Approved | Paid | Cancelled
    AccountingVoucherID INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    ApprovedBy          INT             NULL,
    ApprovedAt          DATETIME2       NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- MODULE K: BUDGET
-- ============================================================

CREATE TABLE acc.Budget (
    BudgetID            INT IDENTITY(1,1) PRIMARY KEY,
    BudgetName          NVARCHAR(150)   NOT NULL,
    FiscalYearID        INT             NOT NULL
        REFERENCES acc.FiscalYear(FiscalYearID),
    BudgetType          NVARCHAR(30)    NOT NULL DEFAULT 'Annual',
    -- Annual | Revised | Supplementary
    Status              NVARCHAR(20)    NOT NULL DEFAULT 'Draft',
    -- Draft | Submitted | Approved | Locked
    ApprovedBy          INT             NULL,
    ApprovedAt          DATETIME2       NULL,
    Notes               NVARCHAR(500)   NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

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
    -- null = annual, set = monthly allocation
    BudgetedAmount      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    RevisedAmount       DECIMAL(18,2)   NULL,
    Remarks             NVARCHAR(300)   NULL
);

-- ============================================================
-- MODULE L: OPENING BALANCES
-- ============================================================

CREATE TABLE acc.OpeningBalance (
    OpeningBalanceID    INT IDENTITY(1,1) PRIMARY KEY,
    FiscalYearID        INT             NOT NULL
        REFERENCES acc.FiscalYear(FiscalYearID),
    AccountHeadID       INT             NOT NULL
        REFERENCES acc.AccountHead(AccountHeadID),
    SubLedgerType       NVARCHAR(30)    NULL,
    SubLedgerID         INT             NULL,
    DebitAmount         DECIMAL(18,2)   NOT NULL DEFAULT 0,
    CreditAmount        DECIMAL(18,2)   NOT NULL DEFAULT 0,
    IsPosted            BIT             NOT NULL DEFAULT 0,
    PostedVoucherID     INT             NULL
        REFERENCES acc.Voucher(VoucherID),
    Notes               NVARCHAR(300)   NULL,
    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- MODULE M: AUDIT LOG (AMS-specific)
-- ============================================================

CREATE TABLE acc.AccountingAuditLog (
    LogID               BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName           NVARCHAR(100)   NOT NULL,
    RecordID            NVARCHAR(50)    NOT NULL,
    Action              NVARCHAR(20)    NOT NULL,
    -- Insert | Update | Delete | Post | Reverse | Approve | Cancel
    OldValues           NVARCHAR(MAX)   NULL,   -- JSON snapshot
    NewValues           NVARCHAR(MAX)   NULL,   -- JSON snapshot
    ChangedBy           INT             NOT NULL,
    ChangedAt           DATETIME2       NOT NULL DEFAULT GETDATE(),
    IPAddress           NVARCHAR(50)    NULL,
    UserAgent           NVARCHAR(300)   NULL
);
```

---

## 4. Seed Data (run after table creation)

```sql
-- Voucher Types
INSERT INTO acc.VoucherType (TypeCode, TypeName, Prefix, IsAutoNumbered) VALUES
('JV',  'Journal Voucher',         'JV-',   1),
('BPV', 'Bank Payment Voucher',    'BPV-',  1),
('BRV', 'Bank Receipt Voucher',    'BRV-',  1),
('CPV', 'Cash Payment Voucher',    'CPV-',  1),
('CRV', 'Cash Receipt Voucher',    'CRV-',  1),
('PDC', 'Post-Dated Cheque',       'PDC-',  1),
('RV',  'Reversal Voucher',        'RV-',   1),
('OBV', 'Opening Balance Voucher', 'OBV-',  1);

-- Account Categories
INSERT INTO acc.AccountCategory (CategoryName, NatureType) VALUES
('Asset',     'Debit'),
('Liability', 'Credit'),
('Equity',    'Credit'),
('Revenue',   'Credit'),
('Expense',   'Debit');

-- Tax Types (Pakistan standard)
-- AccountHeadID references must be inserted after COA is set up
-- These are placeholders — update AccountHeadID after COA setup
INSERT INTO acc.TaxType (TaxCode, TaxName, TaxCategory, AppliesTo, Rate) VALUES
('WHT-RENT',  'WHT on Rent (Section 155)',        'WHT', 'Payable',    15.00),
('WHT-SERV',  'WHT on Services (Section 153)',    'WHT', 'Payable',     8.00),
('WHT-CONTR', 'WHT on Contracts (Section 153)',   'WHT', 'Payable',     7.00),
('WHT-COMM',  'WHT on Commission (Section 233)',  'WHT', 'Payable',    10.00),
('GST-17',    'General Sales Tax 17%',            'GST', 'Both',       17.00),
('GST-13',    'Reduced GST 13%',                  'GST', 'Both',       13.00);
```

---

## 5. Required SQL Views (for reporting)

Create these views. They power all standard accounting reports.

```sql
-- Trial Balance view
CREATE VIEW acc.vw_TrialBalance AS
SELECT
    ah.AccountCode,
    ah.AccountName,
    ac.CategoryName,
    ac.NatureType,
    SUM(vl.DebitAmount)  AS TotalDebit,
    SUM(vl.CreditAmount) AS TotalCredit,
    CASE ac.NatureType
        WHEN 'Debit'  THEN ah.OpeningBalance + SUM(vl.DebitAmount)  - SUM(vl.CreditAmount)
        WHEN 'Credit' THEN ah.OpeningBalance + SUM(vl.CreditAmount) - SUM(vl.DebitAmount)
    END AS ClosingBalance
FROM acc.AccountHead ah
JOIN acc.AccountCategory ac ON ah.AccountCategoryID = ac.AccountCategoryID
LEFT JOIN acc.VoucherLine vl ON ah.AccountHeadID = vl.AccountHeadID
LEFT JOIN acc.Voucher v ON vl.VoucherID = v.VoucherID AND v.Status = 'Posted'
GROUP BY ah.AccountCode, ah.AccountName, ac.CategoryName, ac.NatureType, ah.OpeningBalance;

-- General Ledger view
CREATE VIEW acc.vw_GeneralLedger AS
SELECT
    v.VoucherNo,
    v.VoucherDate,
    vt.TypeCode AS VoucherType,
    ah.AccountCode,
    ah.AccountName,
    vl.SubLedgerType,
    vl.SubLedgerID,
    vl.Description,
    vl.DebitAmount,
    vl.CreditAmount,
    v.Narration,
    v.PMSCustomerID,
    v.PMSProjectID,
    v.Status
FROM acc.VoucherLine vl
JOIN acc.Voucher v      ON vl.VoucherID      = v.VoucherID
JOIN acc.VoucherType vt ON v.VoucherTypeID   = vt.VoucherTypeID
JOIN acc.AccountHead ah ON vl.AccountHeadID  = ah.AccountHeadID;

-- AR Aging view (30/60/90/120+ days)
CREATE VIEW acc.vw_ARAgeing AS
SELECT
    inv.ARInvoiceID,
    inv.InvoiceNo,
    inv.InvoiceDate,
    inv.DueDate,
    inv.CustomerID,
    inv.InvoiceType,
    inv.TotalAmount,
    inv.PaidAmount,
    inv.BalanceAmount,
    DATEDIFF(DAY, inv.DueDate, GETDATE()) AS DaysOverdue,
    CASE
        WHEN DATEDIFF(DAY, inv.DueDate, GETDATE()) <= 0    THEN 'Current'
        WHEN DATEDIFF(DAY, inv.DueDate, GETDATE()) <= 30   THEN '1-30 Days'
        WHEN DATEDIFF(DAY, inv.DueDate, GETDATE()) <= 60   THEN '31-60 Days'
        WHEN DATEDIFF(DAY, inv.DueDate, GETDATE()) <= 90   THEN '61-90 Days'
        WHEN DATEDIFF(DAY, inv.DueDate, GETDATE()) <= 120  THEN '91-120 Days'
        ELSE '120+ Days'
    END AS AgeingBucket
FROM acc.ARInvoice inv
WHERE inv.Status NOT IN ('Paid', 'Cancelled');

-- AP Aging view
CREATE VIEW acc.vw_APAgeing AS
SELECT
    b.APBillID,
    b.BillNo,
    b.BillDate,
    b.DueDate,
    b.VendorID,
    b.BillType,
    b.TotalAmount,
    b.PaidAmount,
    b.BalanceAmount,
    DATEDIFF(DAY, b.DueDate, GETDATE()) AS DaysOverdue,
    CASE
        WHEN DATEDIFF(DAY, b.DueDate, GETDATE()) <= 0    THEN 'Current'
        WHEN DATEDIFF(DAY, b.DueDate, GETDATE()) <= 30   THEN '1-30 Days'
        WHEN DATEDIFF(DAY, b.DueDate, GETDATE()) <= 60   THEN '31-60 Days'
        WHEN DATEDIFF(DAY, b.DueDate, GETDATE()) <= 90   THEN '61-90 Days'
        ELSE '90+ Days'
    END AS AgeingBucket
FROM acc.APBill b
WHERE b.Status NOT IN ('Paid', 'Cancelled');

-- PDC Due Today (for scheduled job)
CREATE VIEW acc.vw_PDCDueToday AS
SELECT cr.*
FROM acc.ChequeRegister cr
WHERE cr.IsPostDated = 1
  AND cr.Status = 'Pending'
  AND cr.ChequeDate <= CAST(GETDATE() AS DATE);

-- Budget vs Actual
CREATE VIEW acc.vw_BudgetVsActual AS
SELECT
    bl.BudgetID,
    bl.AccountHeadID,
    ah.AccountName,
    bl.CostCenterID,
    bl.PeriodID,
    ISNULL(bl.RevisedAmount, bl.BudgetedAmount) AS BudgetAmount,
    ISNULL(SUM(vl.DebitAmount) - SUM(vl.CreditAmount), 0) AS ActualAmount,
    ISNULL(bl.RevisedAmount, bl.BudgetedAmount)
        - ISNULL(SUM(vl.DebitAmount) - SUM(vl.CreditAmount), 0) AS Variance
FROM acc.BudgetLine bl
JOIN acc.AccountHead ah ON bl.AccountHeadID = ah.AccountHeadID
LEFT JOIN acc.VoucherLine vl ON bl.AccountHeadID = vl.AccountHeadID
LEFT JOIN acc.Voucher v ON vl.VoucherID = v.VoucherID AND v.Status = 'Posted'
GROUP BY bl.BudgetID, bl.AccountHeadID, ah.AccountName,
         bl.CostCenterID, bl.PeriodID,
         bl.BudgetedAmount, bl.RevisedAmount;
```

---

## 6. Business Rules & Constraints (enforce in application layer)

### Voucher Engine Rules
- A voucher cannot be posted unless `TotalDebit = TotalCredit` (balanced entry)
- A posted voucher cannot be edited — only reversed via a new `RV` voucher
- Voucher date must fall within an `Open` accounting period
- Auto-generated vouchers (`IsAutoGenerated = true`) must still go through approval if amount > threshold (configurable in `dbo.Configuration`)
- Voucher number is system-assigned and sequential per `VoucherType` per `FiscalYear`

### Period & Year Rules
- No posting allowed to a `Closed` or `Locked` period
- Period close requires: all vouchers in `Posted` or `Cancelled` status (no `Draft` or `Pending`)
- Year close generates opening balance entries for next year automatically

### AR Rules
- An `ARInvoice` is auto-generated from `dbo.PaymentSchedule` when installment due date arrives
- Receipt allocation cannot exceed invoice balance
- PDC receipts are recorded immediately but `ARInvoice` is only marked paid after cheque clears
- Cheque bounce reverses the ARReceipt and re-opens the ARInvoice

### AP Rules
- APBill requires approval before payment (`Status = 'Approved'`)
- Retention is released separately via `APPaymentAllocation.IsRetentionRelease = true`
- WHT is deducted at source at time of APPayment and creates a `TaxTransaction` entry

### Bank Rules
- PDC register: `IsPostDated = true` cheques show in PDC report; a daily job checks `vw_PDCDueToday` and alerts accounts staff
- Bank reconciliation must be done monthly before period close
- Cash account cannot go negative (validate before posting)

### Tax Rules
- WHT deduction is mandatory on all vendor payments above PKR 30,000 (configurable)
- WHT certificate must be issued to vendor within 30 days of deduction
- GST input/output reconciliation is monthly

---

## 7. PMS → AMS Integration Events

These are the PMS events that must automatically create AMS vouchers.
Implement as application-layer service calls (preferred) or SQL triggers (simpler but harder to maintain).

| PMS Event | Trigger Point | AMS Action | Voucher Type |
|---|---|---|---|
| Payment received | `dbo.Payments` INSERT | Create `ARReceipt` + post `BRV` | BRV |
| Installment due | `dbo.PaymentSchedule` due date | Create `ARInvoice` | — (invoice only) |
| Penalty raised | `dbo.Penalties` INSERT | Create `ARInvoice` (type: Penalty) | — |
| Penalty waived | `dbo.Waiver` INSERT | Cancel/reduce `ARInvoice` | JV |
| Dealer payment approved | `dbo.DealerPayments` status change | Create `DealerCommissionVoucher` | BPV |
| Refund approved | `dbo.Refunds` status = Approved | Create `RefundVoucher` | BPV |
| Transfer fee paid | `dbo.TransferFee` INSERT | Create `ARReceipt` + `BRV` | BRV |
| Rental payment | `dbo.RentalPayments` INSERT | Create `ARReceipt` + `BRV` | BRV |
| NDC fee | `dbo.NDC` status change | Create `ARInvoice` | — |
| Registration fee | `dbo.Registration` INSERT | Create `ARInvoice` | — |

---

## 8. Default Chart of Accounts Structure

The accounts team must configure this on first setup. Provide a COA import screen.
Suggested top-level structure for a real estate company:

```
1000  ASSETS
  1100  Current Assets
    1110  Cash in Hand
    1120  Bank Accounts          ← one sub-account per BankAccount
    1130  Post-Dated Cheques Receivable
    1140  Accounts Receivable – Customers
    1150  Advance Tax (Income Tax)
    1160  Input Tax (GST Claimable)
  1200  Fixed Assets
    1210  Land & Plots (Inventory)
    1220  Buildings Under Construction
    1230  Office Equipment
    1240  Vehicles
    1290  Accumulated Depreciation

2000  LIABILITIES
  2100  Current Liabilities
    2110  Accounts Payable – Vendors
    2120  Contractor Retention Payable
    2130  WHT Payable (to FBR)
    2140  GST Payable (Output Tax)
    2150  Advance from Customers
    2160  Post-Dated Cheques Payable
  2200  Long-term Liabilities
    2210  Bank Loans

3000  EQUITY
  3100  Paid-up Capital
  3200  Retained Earnings
  3300  Current Year Profit/Loss

4000  REVENUE
  4100  Plot Sales Revenue
  4200  Building/Unit Sales Revenue
  4300  Rental Income
  4400  Transfer Fee Income
  4500  NDC Fee Income
  4600  Registration Fee Income
  4700  Penalty Income
  4800  Other Income

5000  EXPENSES
  5100  Construction Costs
  5200  Land Acquisition Cost
  5300  Marketing & Commission
    5310  Dealer Commission Expense
  5400  Administrative Expenses
    5410  Salaries & Wages
    5420  Office Rent
    5430  Utilities
    5440  Legal & Professional
  5500  Finance Costs
    5510  Bank Charges
    5520  Cheque Bounce Charges
  5600  Taxes & Duties
```

---

## 9. Module-wise UI Screens Required

### Setup Screens
- Chart of Accounts (tree view, CRUD, import from Excel)
- Fiscal Year & Periods management
- Bank Account setup
- Cheque Book management
- Cost Center setup (linked to PMS Projects)
- Vendor master (CRUD)
- Tax Type configuration
- Opening Balance entry screen
- Voucher Type configuration

### Transaction Screens
- **Journal Voucher** entry (manual double-entry, multi-line)
- **Bank Payment Voucher** (with cheque selection)
- **Bank Receipt Voucher** (with cheque/PDC entry)
- **Cash Payment / Receipt Voucher**
- **AR Invoice** (manual creation + view auto-generated)
- **AR Receipt** with invoice allocation
- **AP Bill** entry with line items
- **AP Payment** with bill allocation
- **PDC Register** (view due PDCs, mark cleared/bounced)
- **Bank Reconciliation** (statement upload + line matching)
- **Dealer Commission** voucher approval
- **Refund Voucher** approval

### Report Screens
- Trial Balance (with date filter, project filter)
- General Ledger (per account, date range, sub-ledger drill-down)
- Account Statement (customer / vendor / dealer)
- AR Aging Report (30/60/90/120+ days, project-wise)
- AP Aging Report
- Cash Flow Statement (monthly, project-wise)
- Project-wise P&L
- PDC Due Report
- Bank Reconciliation Report
- WHT Summary Report (FBR format)
- GST Input/Output Report
- Budget vs Actual Report
- Voucher Listing (by type, date range, status)

---

## 10. User Roles & Permissions for AMS

Add these as new module permission entries in `dbo.UserModulePermission`:

| Role | Permissions |
|---|---|
| Accounts Officer | Voucher entry (Draft), AR/AP entry, view reports |
| Accounts Manager | All above + Approve vouchers, post vouchers, bank reconciliation |
| Finance Director | All above + Fiscal year/period management, budget approval, COA edit |
| CEO / Owner | Reports only (read-only access to all reports) |
| IT Admin | Full access including configuration |

---

## 11. Scheduled Jobs Required

| Job | Frequency | Action |
|---|---|---|
| PDC Due Alert | Daily (8 AM) | Query `vw_PDCDueToday`, notify accounts manager |
| Installment Invoice Generator | Daily (midnight) | Query `dbo.PaymentSchedule` for due dates, create `acc.ARInvoice` |
| WHT Certificate Reminder | Monthly (1st) | Flag TaxTransactions without WHTCertificate older than 25 days |
| Period Close Reminder | Monthly (25th) | Alert accounts manager to close previous month |
| Budget Variance Alert | Weekly | Flag budget lines where actual > 90% of budget |

---

## 12. Technology Stack Assumptions

> Update this section to match your actual PMS stack.

- **Backend**: (same as PMS — .NET / Node.js / Laravel — specify)
- **ORM**: Entity Framework / Dapper / raw SQL (specify — if EF, AMS tables need model classes in `acc` schema)
- **Frontend**: (same as PMS frontend — specify)
- **Database**: MS SQL Server (same instance, same database `PMSAbbas`)
- **Schema**: `acc` (separate from PMS `dbo`)
- **Authentication**: Reuse existing PMS auth — `dbo.Users`, `dbo.UserSessions`, `dbo.UserModulePermission`
- **File attachments**: Reuse `dbo.Attachments` table from PMS for bill/voucher attachments

---

## 13. Development Phases

### Phase 1 — Foundation (Week 1–2)
1. Create `acc` schema and all tables
2. Insert seed data (VoucherTypes, AccountCategories, TaxTypes)
3. COA setup screen + default COA import
4. Fiscal Year and Period management
5. Bank Account setup
6. Opening Balance entry

### Phase 2 — Core Voucher Engine (Week 3–4)
1. Manual Journal Voucher entry (balanced validation)
2. Bank Payment / Receipt Voucher
3. Cash Payment / Receipt Voucher
4. Voucher approval workflow (Draft → Pending → Approved → Posted)
5. Voucher reversal
6. General Ledger view
7. Trial Balance report

### Phase 3 — AR Module (Week 5–6)
1. ARInvoice auto-generation from `dbo.PaymentSchedule`
2. ARInvoice manual creation
3. ARReceipt with invoice allocation
4. PDC receipt handling
5. Cheque bounce workflow
6. AR Aging report
7. Customer account statement

### Phase 4 — AP Module (Week 7–8)
1. Vendor master
2. APBill entry with line items
3. APBill approval workflow
4. APPayment with bill allocation
5. Retention hold & release
6. WHT deduction on payment
7. AP Aging report
8. Vendor account statement

### Phase 5 — PMS Integration (Week 9–10)
1. Payment event → BRV auto-generation
2. DealerPayment event → DealerCommissionVoucher
3. Refund event → RefundVoucher
4. Penalty event → ARInvoice
5. Transfer/Rental/NDC/Registration → ARInvoice
6. Integration test: end-to-end payment flow

### Phase 6 — Bank, Tax & Reports (Week 11–12)
1. Bank Reconciliation screen
2. PDC Register and daily job
3. WHT Certificate generation
4. GST Input/Output report
5. Cash Flow Statement
6. Project-wise P&L
7. Budget entry and Budget vs Actual report

### Phase 7 — QA & Go-Live (Week 13–14)
1. UAT with accounts team
2. Opening balance migration from existing records
3. Parallel run (1 month): verify AMS vs manual books
4. Training for accounts department
5. Go-live

---

## 14. Critical Business Scenarios to Test

1. **Full installment flow**: PaymentSchedule due → ARInvoice created → Customer pays → ARReceipt → BRV posted → Invoice marked Paid
2. **PDC flow**: PDC received → ChequeRegister (IsPostDated=true) → due date arrives → cheque presented → cleared → BRV posted
3. **Cheque bounce**: ChequeRegister status → Bounced → BounceVoucherID reversal JV → ARInvoice re-opened → penalty ARInvoice raised
4. **Dealer commission**: DealerPayment approved in PMS → DealerCommissionVoucher → WHT deducted → NetPayable posted as BPV → WHTCertificate issued
5. **Refund**: Refund approved in PMS → RefundVoucher → deductions applied → BPV posted → customer account cleared
6. **Vendor bill with retention**: APBill entered → 10% retention held → partial payment posted → project complete → retention released via APPaymentAllocation
7. **Period close**: All vouchers posted → AR/AP reconciled → bank reconciliation done → period closed → no further posting allowed
8. **Budget overrun**: Expense posting triggers check against BudgetLine → alert if > budget

---

## 15. Notes for Cursor AI

- All AMS tables are in `acc` schema. Always prefix with `acc.` in queries and migrations.
- PMS tables are in `dbo` schema. Never write to them from AMS code — read-only via SELECT or views.
- `CreatedBy`, `ApprovedBy`, `PostedBy` etc. all reference `dbo.Users.UserID` — no FK constraint declared (cross-schema) but validate in application layer.
- All money columns use `DECIMAL(18,2)` — never `FLOAT` or `MONEY` type.
- `BalanceAmount` on ARInvoice and APBill are computed columns (`AS`) — do not include them in INSERT/UPDATE statements.
- Voucher posting must be atomic: update `acc.Voucher.Status = 'Posted'` and all `acc.VoucherLine` records in a single transaction.
- Always filter vouchers by `Status = 'Posted'` in financial reports — Draft/Pending vouchers must not appear in balances.
- Sequence/auto-number for `VoucherNo` should be: `{TypeCode}-{FiscalYearShort}-{NNNNNN}` e.g. `BRV-25-000142`
- The `SubLedgerType` + `SubLedgerID` pattern is a polymorphic reference — it allows one `VoucherLine` to link to Customer, Dealer, Vendor, or Employee without separate FK columns for each. Resolve in application layer.
- EF migrations: place all `acc.*` entity configurations in a separate `AccountingDbContext` or as a separate assembly if the project supports it, to keep AMS code isolated from PMS code.



