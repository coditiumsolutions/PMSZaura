using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("TaxTransaction", Schema = "acc")]
public class AccTaxTransaction
{
    public int TaxTransactionID { get; set; }

    public int VoucherID { get; set; }

    public int TaxTypeID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxableAmount { get; set; }

    [Column(TypeName = "decimal(7,4)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [StringLength(30)]
    public string? SubLedgerType { get; set; }

    [StringLength(10)]
    public string? SubLedgerID { get; set; }

    public int PeriodID { get; set; }

    [StringLength(50)]
    public string? ChallanNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DepositedDate { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(VoucherID))]
    public AccVoucher? Voucher { get; set; }

    [ForeignKey(nameof(TaxTypeID))]
    public AccTaxType? TaxType { get; set; }

    [ForeignKey(nameof(PeriodID))]
    public AccAccountingPeriod? Period { get; set; }
}
