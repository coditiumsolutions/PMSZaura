using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("TaxType", Schema = "acc")]
public class AccTaxType
{
    public int TaxTypeID { get; set; }

    [StringLength(20)]
    public string TaxCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string TaxName { get; set; } = string.Empty;

    [StringLength(30)]
    public string TaxCategory { get; set; } = string.Empty;

    [StringLength(30)]
    public string AppliesTo { get; set; } = string.Empty;

    [Column(TypeName = "decimal(7,4)")]
    public decimal Rate { get; set; }

    public int AccountHeadID { get; set; }

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "date")]
    public DateTime? EffectiveFrom { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EffectiveTo { get; set; }

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }
}
