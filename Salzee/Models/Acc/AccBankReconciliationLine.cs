using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("BankReconciliationLine", Schema = "acc")]
public class AccBankReconciliationLine
{
    public int ReconLineID { get; set; }

    public int ReconciliationID { get; set; }

    public int? VoucherLineID { get; set; }

    public int? ChequeRegisterID { get; set; }

    [Column(TypeName = "date")]
    public DateTime TransactionDate { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public bool IsReconciled { get; set; }

    public DateTime? ReconciledAt { get; set; }

    [ForeignKey(nameof(ReconciliationID))]
    public AccBankReconciliation? Reconciliation { get; set; }

    [ForeignKey(nameof(VoucherLineID))]
    public AccVoucherLine? VoucherLine { get; set; }

    [ForeignKey(nameof(ChequeRegisterID))]
    public AccChequeRegister? ChequeRegister { get; set; }
}
