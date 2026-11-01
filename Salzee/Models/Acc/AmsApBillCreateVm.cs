namespace Salzee.Models.Acc;

public class AmsApBillLineInputVm
{
    public int AccountHeadID { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

public class AmsApBillCreateVm
{
    public string BillNo { get; set; } = string.Empty;

    public DateTime BillDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);

    public int VendorID { get; set; }

    public string? ProjectID { get; set; }

    public string BillType { get; set; } = "Invoice";

    public decimal WHTAmount { get; set; }

    public decimal GSTAmount { get; set; }

    public decimal OtherTaxAmount { get; set; }

    public decimal RetentionAmount { get; set; }

    public string? Notes { get; set; }

    public List<AmsApBillLineInputVm> Lines { get; set; } = new();
}

public class AmsApAllocationLineVm
{
    public int APBillID { get; set; }

    public decimal Amount { get; set; }

    public bool IsRetentionRelease { get; set; }
}

public class AmsApPaymentCreateVm
{
    public int VendorID { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    public decimal PaidAmount { get; set; }

    public string PaymentMode { get; set; } = "Bank";

    public int? BankAccountID { get; set; }

    public string? Remarks { get; set; }

    public List<AmsApAllocationLineVm> Allocations { get; set; } = new();
}

public class AmsApAgingRowVm
{
    public int APBillId { get; set; }

    public string BillNo { get; set; } = string.Empty;

    public int VendorId { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public decimal Total { get; set; }

    public decimal Paid { get; set; }

    public decimal Retention { get; set; }

    public decimal Balance { get; set; }

    public int DaysPastDue { get; set; }
}
