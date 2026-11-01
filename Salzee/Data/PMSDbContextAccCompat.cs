namespace Salzee.Data;

/// <summary>
/// Set from Program.cs before DbContext is built. When false, acc.Voucher.BankAccountID is not mapped (legacy DBs before Scripts/AMS_Alter_Voucher_BankAccount.sql).
/// </summary>
public static class PMSDbContextAccCompat
{
    public static bool MapVoucherBankAccountColumn { get; set; } = true;
}
