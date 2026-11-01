namespace Salzee.Models.Acc;

public class AmsJvLineInputVm
{
    public int AccountHeadID { get; set; }

    public decimal DebitAmount { get; set; }

    public decimal CreditAmount { get; set; }

    public string? Description { get; set; }
}

public class AmsJvEditVm
{
    public int? VoucherID { get; set; }

    public int FiscalYearID { get; set; }

    public DateTime VoucherDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Narration { get; set; }

    public List<AmsJvLineInputVm> Lines { get; set; } = new();
}
