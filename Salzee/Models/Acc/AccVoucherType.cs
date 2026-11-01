using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("VoucherType", Schema = "acc")]
public class AccVoucherType
{
    public int VoucherTypeID { get; set; }

    [StringLength(10)]
    public string TypeCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string TypeName { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Prefix { get; set; }

    public bool IsAutoNumbered { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<AccVoucher> Vouchers { get; set; } = new List<AccVoucher>();
}
