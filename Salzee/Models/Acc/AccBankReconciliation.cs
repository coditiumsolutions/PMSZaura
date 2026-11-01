using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("BankReconciliation", Schema = "acc")]
public class AccBankReconciliation
{
    public int ReconciliationID { get; set; }

    public int BankAccountID { get; set; }

    public int PeriodID { get; set; }

    [Column(TypeName = "date")]
    public DateTime StatementDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BankStatementBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BookBalance { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(10)]
    public string? ReconciledBy { get; set; }

    public DateTime? ReconciledAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(BankAccountID))]
    public AccBankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(PeriodID))]
    public AccAccountingPeriod? Period { get; set; }

    public ICollection<AccBankReconciliationLine> Lines { get; set; } = new List<AccBankReconciliationLine>();

    [NotMapped]
    public decimal Difference => BankStatementBalance - BookBalance;
}
