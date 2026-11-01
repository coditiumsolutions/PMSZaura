using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("DealerCommissionVoucher", Schema = "acc")]
public class AccDealerCommissionVoucher
{
    public int CommissionVoucherID { get; set; }

    [StringLength(30)]
    public string VoucherNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime VoucherDate { get; set; }

    public int DealerID { get; set; }

    [StringLength(10)]
    public string? ProjectID { get; set; }

    [StringLength(10)]
    public string? AllotmentID { get; set; }

    [StringLength(10)]
    public string? PMSDealerPaymentID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossCommission { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal WHTRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WHTAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetPayable { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public int? AccountingVoucherID { get; set; }

    public int? APPaymentID { get; set; }

    [StringLength(10)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(AccountingVoucherID))]
    public AccVoucher? AccountingVoucher { get; set; }

    [ForeignKey(nameof(APPaymentID))]
    public AccAPPayment? APPayment { get; set; }
}
