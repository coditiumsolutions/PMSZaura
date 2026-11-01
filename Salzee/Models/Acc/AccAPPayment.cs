using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("APPayment", Schema = "acc")]
public class AccAPPayment
{
    public int APPaymentID { get; set; }

    [StringLength(30)]
    public string PaymentNo { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime PaymentDate { get; set; }

    public int VendorID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [StringLength(30)]
    public string PaymentMode { get; set; } = "Bank";

    public int? BankAccountID { get; set; }

    public int? ChequeRegisterID { get; set; }

    [StringLength(30)]
    public string? ChequeNo { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ChequeDate { get; set; }

    public int? VoucherID { get; set; }

    [ForeignKey(nameof(VoucherID))]
    public AccVoucher? Voucher { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Posted";

    [StringLength(500)]
    public string? Remarks { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(VendorID))]
    public AccVendor? Vendor { get; set; }

    [ForeignKey(nameof(BankAccountID))]
    public AccBankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(ChequeRegisterID))]
    public AccChequeRegister? ChequeRegister { get; set; }

    public ICollection<AccAPPaymentAllocation> Allocations { get; set; } = new List<AccAPPaymentAllocation>();
}
