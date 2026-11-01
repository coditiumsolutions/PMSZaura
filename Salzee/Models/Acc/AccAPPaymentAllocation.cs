using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("APPaymentAllocation", Schema = "acc")]
public class AccAPPaymentAllocation
{
    public int AllocationID { get; set; }

    public int APPaymentID { get; set; }

    public int APBillID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AllocatedAmount { get; set; }

    public bool IsRetentionRelease { get; set; }

    public DateTime AllocatedAt { get; set; }

    [StringLength(10)]
    public string AllocatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(APPaymentID))]
    public AccAPPayment? Payment { get; set; }

    [ForeignKey(nameof(APBillID))]
    public AccAPBill? Bill { get; set; }
}
