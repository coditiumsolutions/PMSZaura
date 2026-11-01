using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("VoucherLine", Schema = "acc")]
public class AccVoucherLine
{
    public int VoucherLineID { get; set; }

    public int VoucherID { get; set; }

    public short LineNumber { get; set; }

    public int AccountHeadID { get; set; }

    [StringLength(30)]
    public string? SubLedgerType { get; set; }

    [StringLength(10)]
    public string? SubLedgerID { get; set; }

    public int? CostCenterID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DebitAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FxAmount { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal? ExchangeRate { get; set; }

    [StringLength(10)]
    public string? Currency { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool Reconciled { get; set; }

    [ForeignKey(nameof(VoucherID))]
    public AccVoucher? Voucher { get; set; }

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }
}
