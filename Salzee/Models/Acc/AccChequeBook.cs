using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("ChequeBook", Schema = "acc")]
public class AccChequeBook
{
    public int ChequeBookID { get; set; }

    public int BankAccountID { get; set; }

    [StringLength(20)]
    public string SeriesFrom { get; set; } = string.Empty;

    [StringLength(20)]
    public string SeriesTo { get; set; } = string.Empty;

    public int TotalLeaves { get; set; }

    public int UsedLeaves { get; set; }

    [Column(TypeName = "date")]
    public DateTime IssuedDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(BankAccountID))]
    public AccBankAccount? BankAccount { get; set; }

    public ICollection<AccChequeRegister> ChequeRegisters { get; set; } = new List<AccChequeRegister>();
}
