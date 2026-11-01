/*
  Dummy seed for AMS Step 5 / 6 (idempotent where possible).
  Run after: AMS_Create_acc_schema.sql, COA/bank setup, dbo.Customers populated.
  Also run AMS_Alter_Voucher_BankAccount.sql if using BankAccountID on acc.Voucher.
*/
SET NOCOUNT ON;

DECLARE @Cust NVARCHAR(10) =
    (SELECT TOP (1) LTRIM(RTRIM(CustomerID)) FROM dbo.Customers ORDER BY CustomerID);

IF @Cust IS NULL
BEGIN
    RAISERROR(N'AMS_Step56_Seed: no dbo.Customers row; skip.', 10, 1);
    RETURN;
END;

DECLARE @Head INT =
    (SELECT TOP (1) AccountHeadID FROM acc.AccountHead WHERE IsActive = 1 AND AllowDirectPosting = 1 ORDER BY AccountHeadID);

IF @Head IS NULL
BEGIN
    RAISERROR(N'AMS_Step56_Seed: no posting AccountHead; skip.', 10, 1);
    RETURN;
END;

DECLARE @BankId INT =
    (SELECT TOP (1) BankAccountID FROM acc.BankAccount WHERE IsActive = 1 ORDER BY BankAccountID);

IF @BankId IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM acc.ChequeRegister WHERE ChequeNo = N'SEED-CHQ-PDC-526')
BEGIN
    INSERT INTO acc.ChequeRegister (
        BankAccountID, ChequeBookID, ChequeNo, ChequeDate, EntryDate, IsPostDated,
        ChequeType, Amount, Status, VoucherID, Remarks, CreatedBy)
    VALUES (
        @BankId, NULL, N'SEED-CHQ-PDC-526', DATEADD(DAY, 45, CAST(GETDATE() AS DATE)), CAST(GETDATE() AS DATE), 1,
        N'Payment', 125000.00, N'Pending', NULL, N'Seed Step 5 PDC (dummy)', N'SEED');
END;

IF NOT EXISTS (SELECT 1 FROM acc.ARInvoice WHERE InvoiceNo = N'SEED-AR-526-001')
BEGIN
    INSERT INTO acc.ARInvoice (
        InvoiceNo, InvoiceDate, DueDate, CustomerID, ProjectID, AllotmentID, AccountHeadID,
        InvoiceType, SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAmount, Status,
        PMSPaymentScheduleID, PMSPenaltyID, Notes, CreatedBy)
    VALUES (
        N'SEED-AR-526-001', CAST(GETDATE() AS DATE), DATEADD(DAY, 30, CAST(GETDATE() AS DATE)), @Cust, NULL, NULL, @Head,
        N'Misc', 50000, 0, 0, 50000, 0, N'Unpaid',
        N'SCH-0001', NULL, N'Seed Step 6 unpaid invoice', N'SEED');
END;

IF NOT EXISTS (SELECT 1 FROM acc.ARInvoice WHERE InvoiceNo = N'SEED-AR-526-002')
BEGIN
    INSERT INTO acc.ARInvoice (
        InvoiceNo, InvoiceDate, DueDate, CustomerID, ProjectID, AllotmentID, AccountHeadID,
        InvoiceType, SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAmount, Status,
        PMSPaymentScheduleID, PMSPenaltyID, Notes, CreatedBy)
    VALUES (
        N'SEED-AR-526-002', CAST(GETDATE() AS DATE), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), @Cust, NULL, NULL, @Head,
        N'Misc', 30000, 0, 0, 30000, 0, N'Unpaid',
        NULL, N'PEN-0001', N'Seed Step 6 (past due for aging)', N'SEED');
END;

DECLARE @Inv2 INT = (SELECT ARInvoiceID FROM acc.ARInvoice WHERE InvoiceNo = N'SEED-AR-526-002');

IF @Inv2 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM acc.ARReceipt WHERE ReceiptNo = N'SEED-AR-RC-526-01')
BEGIN
    INSERT INTO acc.ARReceipt (
        ReceiptNo, ReceiptDate, CustomerID, ReceivedAmount, PaymentMode, BankAccountID,
        IsPostDated, Status, Remarks, CreatedBy)
    VALUES (
        N'SEED-AR-RC-526-01', CAST(GETDATE() AS DATE), @Cust, 10000, N'Bank', @BankId,
        0, N'Active', N'Seed partial receipt', N'SEED');

    DECLARE @Rid INT = SCOPE_IDENTITY();

    INSERT INTO acc.ARReceiptAllocation (ARReceiptID, ARInvoiceID, AllocatedAmount, AllocatedBy)
    VALUES (@Rid, @Inv2, 10000, N'SEED');

    UPDATE acc.ARInvoice
    SET PaidAmount = 10000, Status = N'PartiallyPaid'
    WHERE ARInvoiceID = @Inv2;
END;

PRINT N'AMS_Step56_Seed: done (cheque row if bank exists, two AR invoices, one partial receipt).';
