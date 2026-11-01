using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Refund")]
    public class Refund
    {
        [Key]
        [StringLength(10)]
        public string RefundID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CustomerID { get; set; }

        /// <summary>Full or Partial</summary>
        [StringLength(50)]
        public string? RefundType { get; set; }

        /// <summary>Sum of amounts of the selected payments</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        /// <summary>Deduction from the paid amount (e.g. processing fees)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeductionAmount { get; set; } = 0;

        /// <summary>PaidAmount - DeductionAmount</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundedAmount { get; set; }

        [StringLength(255)]
        public string? Reason { get; set; }

        /// <summary>Initiated | Approved | Declined</summary>
        [StringLength(100)]
        public string? WorkflowStatus { get; set; } = "Initiated";

        /// <summary>JSON array of PaymentIDs selected for refund e.g. ["PAY0000001","PAY0000002"]</summary>
        public string? SelectedPaymentIDs { get; set; }

        [StringLength(10)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(10)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<RefundCheque> RefundCheques { get; set; } = new List<RefundCheque>();
    }
}
