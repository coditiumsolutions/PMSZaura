using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("AccountingPeriod", Schema = "acc")]
public class AccAccountingPeriod
{
    public int PeriodID { get; set; }

    public int FiscalYearID { get; set; }

    [StringLength(50)]
    public string PeriodName { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Open";

    [StringLength(10)]
    public string? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    [ForeignKey(nameof(FiscalYearID))]
    public AccFiscalYear? FiscalYear { get; set; }
}
