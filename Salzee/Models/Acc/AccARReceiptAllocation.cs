using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("ARReceiptAllocation", Schema = "acc")]
public class AccARReceiptAllocation
{
    public int AllocationID { get; set; }

    public int ARReceiptID { get; set; }

    public int ARInvoiceID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AllocatedAmount { get; set; }

    public DateTime AllocatedAt { get; set; }

    [StringLength(10)]
    public string AllocatedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(ARReceiptID))]
    public AccARReceipt? Receipt { get; set; }

    [ForeignKey(nameof(ARInvoiceID))]
    public AccARInvoice? Invoice { get; set; }
}
