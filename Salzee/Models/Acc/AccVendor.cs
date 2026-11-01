using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("Vendor", Schema = "acc")]
public class AccVendor
{
    public int VendorID { get; set; }

    [StringLength(20)]
    public string VendorCode { get; set; } = string.Empty;

    [StringLength(200)]
    public string VendorName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? VendorType { get; set; }

    [StringLength(30)]
    public string? NTN { get; set; }

    [StringLength(30)]
    public string? STRN { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(200)]
    public string? BankAccountTitle { get; set; }

    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [StringLength(150)]
    public string? BankName { get; set; }

    [StringLength(34)]
    public string? IBAN { get; set; }

    public int? AccountHeadID { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    [StringLength(10)]
    public string CreatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountHeadID))]
    public AccAccountHead? AccountHead { get; set; }

    public ICollection<AccAPBill> Bills { get; set; } = new List<AccAPBill>();
}
