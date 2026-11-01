using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("AccountHead", Schema = "acc")]
public class AccAccountHead
{
    public int AccountHeadID { get; set; }

    public int AccountCategoryID { get; set; }

    public int? ParentAccountHeadID { get; set; }

    [StringLength(30)]
    public string AccountCode { get; set; } = string.Empty;

    [StringLength(150)]
    public string AccountName { get; set; } = string.Empty;

    public byte AccountLevel { get; set; } = 1;

    public bool IsControlAccount { get; set; }

    public bool AllowDirectPosting { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OpeningBalance { get; set; }

    [Column(TypeName = "date")]
    public DateTime? OpeningBalanceDate { get; set; }

    [StringLength(10)]
    public string? OpeningBalanceType { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountCategoryID))]
    public AccAccountCategory? Category { get; set; }

    [ForeignKey(nameof(ParentAccountHeadID))]
    public AccAccountHead? Parent { get; set; }

    public ICollection<AccAccountHead> Children { get; set; } = new List<AccAccountHead>();
}
