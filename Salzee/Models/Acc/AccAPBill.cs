using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("APBill", Schema = "acc")]
public class AccAPBill
{
    public int APBillID { get; set; }

    [StringLength(30)]
    public string BillNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime BillDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    public int VendorID { get; set; }

    [StringLength(10)]
    public string? ProjectID { get; set; }

    public int? CostCenterID { get; set; }

    [StringLength(50)]
    public string BillType { get; set; } = "Invoice";

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WHTAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GSTAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtherTaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RetentionAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RetentionReleased { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public int? VoucherID { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? AttachmentPath { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [ForeignKey(nameof(VendorID))]
    public AccVendor? Vendor { get; set; }

    [ForeignKey(nameof(VoucherID))]
    public AccVoucher? Voucher { get; set; }

    [NotMapped]
    public decimal BalanceAmount => TotalAmount - RetentionAmount - PaidAmount;

    public ICollection<AccAPBillLine> Lines { get; set; } = new List<AccAPBillLine>();
}
