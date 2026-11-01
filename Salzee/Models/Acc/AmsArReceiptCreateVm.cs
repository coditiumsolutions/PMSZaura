namespace Salzee.Models.Acc;

public class AmsAllocationLineVm
{
    public int ARInvoiceID { get; set; }

    public decimal Amount { get; set; }
}

public class AmsArReceiptCreateVm
{
    public string CustomerID { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow.Date;

    public decimal ReceivedAmount { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(30)]
    public string PaymentMode { get; set; } = "Bank";

    public int? BankAccountID { get; set; }

    public bool IsPostDated { get; set; }

    public string? Remarks { get; set; }

    /// <summary>When true, link an existing pending cheque or create a new row (same pattern as bank vouchers).</summary>
    public bool UseCheque { get; set; }

    public int? ChequeRegisterID { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(30)]
    public string? NewChequeNo { get; set; }

    public DateTime? NewChequeDate { get; set; }

    public List<AmsAllocationLineVm> Allocations { get; set; } = new();
}
