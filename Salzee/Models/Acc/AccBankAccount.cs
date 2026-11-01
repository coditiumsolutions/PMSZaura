using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("BankAccount", Schema = "acc")]
public class AccBankAccount
{
    public int BankAccountID { get; set; }

    public int AccountHeadID { get; set; }

    [StringLength(150)]
    public string BankName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? BranchName { get; set; }

    [StringLength(20)]
    public string? BranchCode { get; set; }

    [StringLength(200)]
    public string AccountTitle { get; set; } = string.Empty;

    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [StringLength(34)]
    public string? IBAN { get; set; }

    [StringLength(30)]
    public string? AccountType { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "PKR";

    [Column(TypeName = "decimal(18,2)")]
    public decimal OpeningBalance { get; set; }

    [Column(TypeName = "date")]
    public DateTime? OpeningDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }

    public ICollection<AccChequeBook> ChequeBooks { get; set; } = new List<AccChequeBook>();

    public ICollection<AccChequeRegister> ChequeRegisters { get; set; } = new List<AccChequeRegister>();
}
