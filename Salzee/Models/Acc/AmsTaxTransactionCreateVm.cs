namespace Salzee.Models.Acc;

public class AmsTaxTransactionCreateVm
{
    public int VoucherID { get; set; }

    public int TaxTypeID { get; set; }

    public decimal TaxableAmount { get; set; }

    public string? SubLedgerType { get; set; }

    public string? SubLedgerID { get; set; }

    public string? ChallanNo { get; set; }

    public DateTime? DepositedDate { get; set; }
}

public class AmsTaxSummaryRowVm
{
    public string TaxCode { get; set; } = string.Empty;

    public string TaxName { get; set; } = string.Empty;

    public string AppliesTo { get; set; } = string.Empty;

    public decimal TotalTaxable { get; set; }

    public decimal TotalTax { get; set; }

    public int LineCount { get; set; }
}
