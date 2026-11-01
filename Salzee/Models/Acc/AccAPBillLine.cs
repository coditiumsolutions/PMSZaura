using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("APBillLine", Schema = "acc")]
public class AccAPBillLine
{
    public int APBillLineID { get; set; }

    public int APBillID { get; set; }

    public short LineNumber { get; set; }

    public int AccountHeadID { get; set; }

    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,3)")]
    public decimal? Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? WHTRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WHTAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? GSTRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? GSTAmount { get; set; }

    [ForeignKey(nameof(APBillID))]
    public AccAPBill? Bill { get; set; }

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }
}
