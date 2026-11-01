using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("BudgetLine", Schema = "acc")]
public class AccBudgetLine
{
    public int BudgetLineID { get; set; }

    public int BudgetID { get; set; }

    public int AccountHeadID { get; set; }

    public int? CostCenterID { get; set; }

    public int? PeriodID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BudgetedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RevisedAmount { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }

    [ForeignKey(nameof(BudgetID))]
    public AccBudget? Budget { get; set; }

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }

    [ForeignKey(nameof(CostCenterID))]
    public AccCostCenter? CostCenter { get; set; }

    [ForeignKey(nameof(PeriodID))]
    public AccAccountingPeriod? Period { get; set; }
}
