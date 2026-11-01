using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("Budget", Schema = "acc")]
public class AccBudget
{
    public int BudgetID { get; set; }

    [StringLength(150)]
    public string BudgetName { get; set; } = string.Empty;

    public int FiscalYearID { get; set; }

    [StringLength(30)]
    public string BudgetType { get; set; } = "Annual";

    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(10)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(FiscalYearID))]
    public AccFiscalYear? FiscalYear { get; set; }

    public ICollection<AccBudgetLine> Lines { get; set; } = new List<AccBudgetLine>();
}
