using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("CostCenter", Schema = "acc")]
public class AccCostCenter
{
    public int CostCenterID { get; set; }

    [StringLength(20)]
    public string CostCenterCode { get; set; } = string.Empty;

    [StringLength(150)]
    public string CostCenterName { get; set; } = string.Empty;

    public int? ParentCostCenterID { get; set; }

    [StringLength(10)]
    public string? ProjectID { get; set; }

    [StringLength(10)]
    public string? SubProjectID { get; set; }

    [StringLength(30)]
    public string? CostCenterType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BudgetAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;
}
