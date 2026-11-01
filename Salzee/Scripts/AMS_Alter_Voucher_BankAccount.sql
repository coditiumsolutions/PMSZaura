/* Step 5: optional columns on acc.Voucher for bank/cheque linkage (run once per database). */
IF COL_LENGTH(N'acc.Voucher', N'BankAccountID') IS NULL
BEGIN
    ALTER TABLE acc.Voucher ADD BankAccountID INT NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Voucher_BankAccount')
    AND COL_LENGTH(N'acc.Voucher', N'BankAccountID') IS NOT NULL
BEGIN
    ALTER TABLE acc.Voucher WITH CHECK ADD CONSTRAINT FK_Voucher_BankAccount
        FOREIGN KEY (BankAccountID) REFERENCES acc.BankAccount(BankAccountID);
END
GO
