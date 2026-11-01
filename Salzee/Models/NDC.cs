using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("NDC")]
    public class NDC
    {
        [Key]
        [StringLength(10)]
        public string NDCID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CustomerID { get; set; }

        [StringLength(100)]
        public string? NDCType { get; set; }

        [StringLength(500)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? WorkFlowStatus { get; set; }

        public string? Comments { get; set; }

        public DateTime IssuedDate { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NDCExpiryDate { get; set; }

        /// <summary>Sum of PaymentSchedule.Amount due by CreatedAt (schedules with DueDate &lt;= CreatedAt for customer's plan).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalDueAmount { get; set; }

        /// <summary>Sum of Payments.Amount for this customer (total paid to date).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalDueInstallments { get; set; }

        /// <summary>1 if TotalDueAmount == TotalDueInstallments (all dues cleared), else 0.</summary>
        public bool AllPaymentClear { get; set; }

        /// <summary>Remaining dues at creation time: max(TotalDueAmount - TotalDueInstallments, 0).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RemainingDues { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountPerUnit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TransferFeeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PropertySize { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }
    }
}
