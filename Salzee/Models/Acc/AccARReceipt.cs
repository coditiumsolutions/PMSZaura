using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("ARReceipt", Schema = "acc")]
public class AccARReceipt
{
    public int ARReceiptID { get; set; }

    [StringLength(30)]
    public string ReceiptNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime ReceiptDate { get; set; }

    [StringLength(10)]
    public string CustomerID { get; set; } = string.Empty;

    [StringLength(10)]
    public string? ProjectID { get; set; }

    [StringLength(10)]
    public string? AllotmentID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ReceivedAmount { get; set; }

    [StringLength(30)]
    public string PaymentMode { get; set; } = "Cash";

    public int? BankAccountID { get; set; }

    public int? ChequeRegisterID { get; set; }

    [StringLength(30)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    [StringLength(150)]
    public string? BankName { get; set; }

    public bool IsPostDated { get; set; }

    [StringLength(10)]
    public string? PMSPaymentID { get; set; }

    public int? VoucherID { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    [StringLength(500)]
    public string? Remarks { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(BankAccountID))]
    public AccBankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(ChequeRegisterID))]
    public AccChequeRegister? ChequeRegister { get; set; }

    public ICollection<AccARReceiptAllocation> Allocations { get; set; } = new List<AccARReceiptAllocation>();
}
