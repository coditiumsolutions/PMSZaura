using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("FiscalYear", Schema = "acc")]
public class AccFiscalYear
{
    public int FiscalYearID { get; set; }

    [StringLength(50)]
    public string YearName { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Open";

    [StringLength(10)]
    public string? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<AccAccountingPeriod> Periods { get; set; } = new List<AccAccountingPeriod>();
}
