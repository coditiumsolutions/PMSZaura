using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("ARInvoice", Schema = "acc")]
public class AccARInvoice
{
    public int ARInvoiceID { get; set; }

    [StringLength(30)]
    public string InvoiceNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime InvoiceDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [StringLength(10)]
    public string CustomerID { get; set; } = string.Empty;

    [StringLength(10)]
    public string? ProjectID { get; set; }

    [StringLength(10)]
    public string? AllotmentID { get; set; }

    public int AccountHeadID { get; set; }

    [StringLength(50)]
    public string InvoiceType { get; set; } = "Misc";

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Unpaid";

    [StringLength(10)]
    public string? PMSPaymentScheduleID { get; set; }

    [StringLength(10)]
    public string? PMSPenaltyID { get; set; }

    public int? VoucherID { get; set; }

    [StringLength(300)]
    public string? CancellationReason { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }

    [NotMapped]
    public decimal BalanceAmount => TotalAmount - PaidAmount;

    public ICollection<AccARReceiptAllocation> ReceiptAllocations { get; set; } = new List<AccARReceiptAllocation>();
}
