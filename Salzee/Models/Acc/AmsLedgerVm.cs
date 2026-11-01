namespace Salzee.Models.Acc;

/// <summary>Fiscal year dropdown for ledger reports — projected columns only (avoids schema drift on production DBs).</summary>
public record AmsFiscalYearOptionVm(int FiscalYearID, string YearName);

public record AmsTbRowVm(int AccountHeadId, string AccountCode, string AccountName, decimal TotalDebit, decimal TotalCredit);

public record AmsGlRowVm(DateTime VoucherDate, string VoucherNo, string TypeCode, short LineNumber, string? Description, decimal DebitAmount, decimal CreditAmount);

public record AmsPostedVoucherRowVm(int VoucherID, DateTime VoucherDate, string VoucherNo, string TypeCode, string TypeName, string Status, decimal TotalDebit, decimal TotalCredit);
