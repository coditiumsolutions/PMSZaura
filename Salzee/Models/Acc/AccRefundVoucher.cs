using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("RefundVoucher", Schema = "acc")]
public class AccRefundVoucher
{
    public int RefundVoucherID { get; set; }

    [StringLength(30)]
    public string VoucherNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime VoucherDate { get; set; }

    [StringLength(10)]
    public string CustomerID { get; set; } = string.Empty;

    [StringLength(10)]
    public string? AllotmentID { get; set; }

    [StringLength(10)]
    public string? PMSRefundID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossRefundAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcessingFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PenaltyDeduction { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtherDeduction { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetRefundAmount { get; set; }

    [StringLength(30)]
    public string PaymentMode { get; set; } = "Bank";

    public int? BankAccountID { get; set; }

    public int? ChequeRegisterID { get; set; }

    [StringLength(30)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public int? AccountingVoucherID { get; set; }

    [StringLength(10)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(BankAccountID))]
    public AccBankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(ChequeRegisterID))]
    public AccChequeRegister? ChequeRegister { get; set; }

    [ForeignKey(nameof(AccountingVoucherID))]
    public AccVoucher? AccountingVoucher { get; set; }
}
