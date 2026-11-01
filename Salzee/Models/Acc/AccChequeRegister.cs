using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("ChequeRegister", Schema = "acc")]
public class AccChequeRegister
{
    public int ChequeRegisterID { get; set; }

    public int BankAccountID { get; set; }

    public int? ChequeBookID { get; set; }

    [StringLength(30)]
    public string ChequeNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime ChequeDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EntryDate { get; set; }

    public bool IsPostDated { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ClearanceDate { get; set; }

    [StringLength(20)]
    public string ChequeType { get; set; } = "Payment";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? PayableTo { get; set; }

    [StringLength(200)]
    public string? ReceivedFrom { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    [StringLength(300)]
    public string? BounceReason { get; set; }

    public int? BounceVoucherID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BounceChargeAmount { get; set; }

    public int? ReplacedByChequeID { get; set; }

    public int? VoucherID { get; set; }

    [ForeignKey(nameof(VoucherID))]
    public AccVoucher? Voucher { get; set; }

    [StringLength(30)]
    public string? SubLedgerType { get; set; }

    [StringLength(10)]
    public string? SubLedgerID { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(BankAccountID))]
    public AccBankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(ChequeBookID))]
    public AccChequeBook? ChequeBook { get; set; }

    [ForeignKey(nameof(ReplacedByChequeID))]
    public AccChequeRegister? ReplacedByCheque { get; set; }
}
