namespace Salzee.Models.Acc;

public class AmsOpeningBalanceIndexVm
{
    public int FiscalYearID { get; set; }
    public List<AmsOpeningBalanceRowVm> Rows { get; set; } = new();
}

public class AmsOpeningBalanceRowVm
{
    public int AccountHeadID { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? OpeningBalanceID { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Notes { get; set; }
    public bool IsPosted { get; set; }
}
