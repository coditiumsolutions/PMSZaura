/*
  AMS schema sanity check — compare your LIVE database to what the app expects (acc schema + Scripts/AMS_Create_acc_schema.sql).

  dbAccounts.txt is a design sketch (often dbo-style names); deployed AMS uses schema **acc** and column names from AMS_Create_acc_schema.sql / AMS_Alter_*.sql.

  Run in SSMS against the same database as ProductionConnection / zkbeclipse.pk.

  If acc.Voucher lacks BankAccountID: run Scripts/AMS_Alter_Voucher_BankAccount.sql OR set appsettings.Production.json AmsAccCompat:MapVoucherBankAccountColumn = false until you add the column.
*/

SET NOCOUNT ON;

PRINT N'--- acc schema exists ---';
IF SCHEMA_ID(N'acc') IS NULL
    PRINT N'FAIL: schema acc does not exist. Run Scripts/AMS_Create_acc_schema.sql';
ELSE
    PRINT N'OK: schema acc exists';

PRINT N'--- core tables ---';
DECLARE @t TABLE (name SYSNAME);
INSERT INTO @t VALUES (N'AccountCategory'),(N'AccountHead'),(N'FiscalYear'),(N'AccountingPeriod'),
    (N'VoucherType'),(N'Voucher'),(N'VoucherLine'),(N'BankAccount');

IF EXISTS (
    SELECT 1 FROM @t t
    WHERE OBJECT_ID(QUOTENAME(N'acc') + N'.' + QUOTENAME(t.name), N'U') IS NULL)
BEGIN
    PRINT N'FAIL: missing acc table(s):';
    SELECT t.name AS MissingTable FROM @t t
    WHERE OBJECT_ID(QUOTENAME(N'acc') + N'.' + QUOTENAME(t.name), N'U') IS NULL;
END
ELSE
    PRINT N'OK: core acc tables present';

PRINT N'--- acc.Voucher columns (BankAccountID) ---';
IF COL_LENGTH(N'acc.Voucher', N'BankAccountID') IS NULL
BEGIN
    PRINT N'WARN: acc.Voucher.BankAccountID missing — EF expects it unless AmsAccCompat:MapVoucherBankAccountColumn=false';
END
ELSE
    PRINT N'OK: acc.Voucher.BankAccountID exists';

PRINT N'--- acc.VoucherLine.LineNumber (not LineNo from older sketches) ---';
IF COL_LENGTH(N'acc.VoucherLine', N'LineNumber') IS NULL
    PRINT N'FAIL: LineNumber missing on acc.VoucherLine';
ELSE
    PRINT N'OK: LineNumber exists';

PRINT N'--- list acc.Voucher columns ---';
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'acc' AND TABLE_NAME = N'Voucher'
ORDER BY ORDINAL_POSITION;
