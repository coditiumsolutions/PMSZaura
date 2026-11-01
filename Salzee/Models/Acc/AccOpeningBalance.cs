using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("OpeningBalance", Schema = "acc")]
public class AccOpeningBalance
{
    public int OpeningBalanceID { get; set; }

    public int FiscalYearID { get; set; }

    public int AccountHeadID { get; set; }

    [StringLength(30)]
    public string? SubLedgerType { get; set; }

    [StringLength(10)]
    public string? SubLedgerID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DebitAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; set; }

    public bool IsPosted { get; set; }

    public int? PostedVoucherID { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(FiscalYearID))]
    public AccFiscalYear? FiscalYear { get; set; }

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }
}
