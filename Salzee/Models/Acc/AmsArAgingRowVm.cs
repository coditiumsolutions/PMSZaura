namespace Salzee.Models.Acc;

public class AmsArAgingRowVm
{
    public int ARInvoiceId { get; set; }

    public string InvoiceNo { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public decimal Total { get; set; }

    public decimal Paid { get; set; }

    public decimal Balance { get; set; }

    public int DaysPastDue { get; set; }
}
