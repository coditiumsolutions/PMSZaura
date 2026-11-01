/*
  Brownfield: widen acc user/PMS bridge columns from INT to NVARCHAR to match dbo / EF (Step 0 ADR).
  Safe to re-run: each ALTER runs only if the column exists and is currently int (or bigint for none here).

  Run after backup. Order avoids touching PMSDealerID / DealerID (remain INT — dbo.Dealers).

  Pair with post-migrate seeds (TaxType / placeholder heads) from AMS_Create_acc_schema.sql tail,
  or run the full create script on empty acc only.
*/
SET NOCOUNT ON;
GO

/* --- acc.AccountHead --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.AccountHead') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.AccountHead ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.FiscalYear --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.FiscalYear') AND c.name = N'ClosedBy' AND t.name = N'int')
    ALTER TABLE acc.FiscalYear ALTER COLUMN ClosedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.FiscalYear') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.FiscalYear ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.AccountingPeriod --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.AccountingPeriod') AND c.name = N'ClosedBy' AND t.name = N'int')
    ALTER TABLE acc.AccountingPeriod ALTER COLUMN ClosedBy NVARCHAR(10) NULL;
GO

/* --- acc.CostCenter --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.CostCenter') AND c.name = N'ProjectID' AND t.name = N'int')
    ALTER TABLE acc.CostCenter ALTER COLUMN ProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.CostCenter') AND c.name = N'SubProjectID' AND t.name = N'int')
    ALTER TABLE acc.CostCenter ALTER COLUMN SubProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.CostCenter') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.CostCenter ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.Voucher --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSProjectID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSAllotmentID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSAllotmentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSCustomerID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSCustomerID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSPaymentID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSPaymentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSRefundID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSRefundID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSDealerPaymentID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSDealerPaymentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSPenaltyID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSPenaltyID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSTransferID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSTransferID NVARCHAR(50) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PMSRentalPaymentID' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PMSRentalPaymentID NVARCHAR(50) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'SubmittedBy' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN SubmittedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'ApprovedBy' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN ApprovedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'PostedBy' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN PostedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Voucher') AND c.name = N'ReversedBy' AND t.name = N'int')
    ALTER TABLE acc.Voucher ALTER COLUMN ReversedBy NVARCHAR(10) NULL;
GO

/* --- acc.VoucherLine --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.VoucherLine') AND c.name = N'SubLedgerID' AND t.name = N'int')
    ALTER TABLE acc.VoucherLine ALTER COLUMN SubLedgerID NVARCHAR(10) NULL;
GO

/* --- acc.BankAccount --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.BankAccount') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.BankAccount ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.ChequeRegister --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ChequeRegister') AND c.name = N'SubLedgerID' AND t.name = N'int')
    ALTER TABLE acc.ChequeRegister ALTER COLUMN SubLedgerID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ChequeRegister') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.ChequeRegister ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.BankReconciliation --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.BankReconciliation') AND c.name = N'ReconciledBy' AND t.name = N'int')
    ALTER TABLE acc.BankReconciliation ALTER COLUMN ReconciledBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.BankReconciliation') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.BankReconciliation ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.ARInvoice --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'CustomerID' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN CustomerID NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'ProjectID' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN ProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'AllotmentID' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN AllotmentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'PMSPaymentScheduleID' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN PMSPaymentScheduleID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'PMSPenaltyID' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN PMSPenaltyID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARInvoice') AND c.name = N'LastModifiedBy' AND t.name = N'int')
    ALTER TABLE acc.ARInvoice ALTER COLUMN LastModifiedBy NVARCHAR(10) NULL;
GO

/* --- acc.ARReceipt --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARReceipt') AND c.name = N'CustomerID' AND t.name = N'int')
    ALTER TABLE acc.ARReceipt ALTER COLUMN CustomerID NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARReceipt') AND c.name = N'ProjectID' AND t.name = N'int')
    ALTER TABLE acc.ARReceipt ALTER COLUMN ProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARReceipt') AND c.name = N'AllotmentID' AND t.name = N'int')
    ALTER TABLE acc.ARReceipt ALTER COLUMN AllotmentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARReceipt') AND c.name = N'PMSPaymentID' AND t.name = N'int')
    ALTER TABLE acc.ARReceipt ALTER COLUMN PMSPaymentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARReceipt') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.ARReceipt ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.ARReceiptAllocation --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.ARReceiptAllocation') AND c.name = N'AllocatedBy' AND t.name = N'int')
    ALTER TABLE acc.ARReceiptAllocation ALTER COLUMN AllocatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.Vendor --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Vendor') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.Vendor ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.APBill --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.APBill') AND c.name = N'ProjectID' AND t.name = N'int')
    ALTER TABLE acc.APBill ALTER COLUMN ProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.APBill') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.APBill ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.APBill') AND c.name = N'ApprovedBy' AND t.name = N'int')
    ALTER TABLE acc.APBill ALTER COLUMN ApprovedBy NVARCHAR(10) NULL;
GO

/* --- acc.APPayment --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.APPayment') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.APPayment ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.APPaymentAllocation --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.APPaymentAllocation') AND c.name = N'AllocatedBy' AND t.name = N'int')
    ALTER TABLE acc.APPaymentAllocation ALTER COLUMN AllocatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.TaxTransaction --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.TaxTransaction') AND c.name = N'SubLedgerID' AND t.name = N'int')
    ALTER TABLE acc.TaxTransaction ALTER COLUMN SubLedgerID NVARCHAR(10) NULL;
GO

/* --- acc.WHTCertificate --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.WHTCertificate') AND c.name = N'SubLedgerID' AND t.name = N'int')
    ALTER TABLE acc.WHTCertificate ALTER COLUMN SubLedgerID NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.WHTCertificate') AND c.name = N'IssuedBy' AND t.name = N'int')
    ALTER TABLE acc.WHTCertificate ALTER COLUMN IssuedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.DealerCommissionVoucher --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.DealerCommissionVoucher') AND c.name = N'ProjectID' AND t.name = N'int')
    ALTER TABLE acc.DealerCommissionVoucher ALTER COLUMN ProjectID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.DealerCommissionVoucher') AND c.name = N'AllotmentID' AND t.name = N'int')
    ALTER TABLE acc.DealerCommissionVoucher ALTER COLUMN AllotmentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.DealerCommissionVoucher') AND c.name = N'PMSDealerPaymentID' AND t.name = N'int')
    ALTER TABLE acc.DealerCommissionVoucher ALTER COLUMN PMSDealerPaymentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.DealerCommissionVoucher') AND c.name = N'ApprovedBy' AND t.name = N'int')
    ALTER TABLE acc.DealerCommissionVoucher ALTER COLUMN ApprovedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.DealerCommissionVoucher') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.DealerCommissionVoucher ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.RefundVoucher --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.RefundVoucher') AND c.name = N'CustomerID' AND t.name = N'int')
    ALTER TABLE acc.RefundVoucher ALTER COLUMN CustomerID NVARCHAR(10) NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.RefundVoucher') AND c.name = N'AllotmentID' AND t.name = N'int')
    ALTER TABLE acc.RefundVoucher ALTER COLUMN AllotmentID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.RefundVoucher') AND c.name = N'PMSRefundID' AND t.name = N'int')
    ALTER TABLE acc.RefundVoucher ALTER COLUMN PMSRefundID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.RefundVoucher') AND c.name = N'ApprovedBy' AND t.name = N'int')
    ALTER TABLE acc.RefundVoucher ALTER COLUMN ApprovedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.RefundVoucher') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.RefundVoucher ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.Budget --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Budget') AND c.name = N'ApprovedBy' AND t.name = N'int')
    ALTER TABLE acc.Budget ALTER COLUMN ApprovedBy NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.Budget') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.Budget ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.OpeningBalance --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.OpeningBalance') AND c.name = N'SubLedgerID' AND t.name = N'int')
    ALTER TABLE acc.OpeningBalance ALTER COLUMN SubLedgerID NVARCHAR(10) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.OpeningBalance') AND c.name = N'CreatedBy' AND t.name = N'int')
    ALTER TABLE acc.OpeningBalance ALTER COLUMN CreatedBy NVARCHAR(10) NOT NULL;
GO

/* --- acc.AccountingAuditLog --- */
IF EXISTS (SELECT 1 FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
           WHERE c.object_id = OBJECT_ID(N'acc.AccountingAuditLog') AND c.name = N'ChangedBy' AND t.name = N'int')
    ALTER TABLE acc.AccountingAuditLog ALTER COLUMN ChangedBy NVARCHAR(10) NOT NULL;
GO

PRINT N'AMS migration: acc INT to NVARCHAR (PMS/user keys) complete where applicable.';
GO
