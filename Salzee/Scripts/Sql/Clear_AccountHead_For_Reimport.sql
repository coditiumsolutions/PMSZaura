-- Clear chart of accounts for fresh CSV import (acc.AccountHead).
-- Run only when there are NO posted vouchers, AR invoices, or AP bills.
-- Safer option: use AmsCoa -> Clear all (re-import) in the app (Admin permission).

SET NOCOUNT ON;

DECLARE @VoucherLines int = (SELECT COUNT(*) FROM acc.VoucherLine);
DECLARE @ArInvoices int = (SELECT COUNT(*) FROM acc.ARInvoice);
DECLARE @ApBills int = (SELECT COUNT(*) FROM acc.APBill);

IF @VoucherLines > 0 OR @ArInvoices > 0 OR @ApBills > 0
BEGIN
    RAISERROR('Blocked: VoucherLine=%d, ARInvoice=%d, APBill=%d. Remove accounting transactions first.', 16, 1, @VoucherLines, @ArInvoices, @ApBills);
    RETURN;
END

BEGIN TRANSACTION;

DELETE FROM acc.OpeningBalance;
DELETE FROM acc.BudgetLine;
DELETE FROM acc.TaxTransaction;
DELETE FROM acc.TaxType;
DELETE FROM acc.BankReconciliationLine;
DELETE FROM acc.BankReconciliation;
DELETE FROM acc.ChequeRegister;
DELETE FROM acc.ChequeBook;
DELETE FROM acc.BankAccount;
UPDATE acc.Vendor SET AccountHeadID = NULL WHERE AccountHeadID IS NOT NULL;
UPDATE acc.AccountHead SET ParentAccountHeadID = NULL;
DELETE FROM acc.AccountHead;

COMMIT TRANSACTION;

SELECT COUNT(*) AS RemainingAccountHeadRows FROM acc.AccountHead;
