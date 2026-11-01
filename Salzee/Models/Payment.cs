using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        [StringLength(10)]
        public string PaymentID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ScheduleID { get; set; }

        [StringLength(10)]
        public string? CustomerID { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public DateTime? DepositDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? Method { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(100)]
        public string? ReferenceNo { get; set; }

        [StringLength(250)]
        public string? Status { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [StringLength(255)]
        public string? AccountHead { get; set; }

        // Audit fields — managed by PaymentAudit module only
        [StringLength(50)]
        public string? AuditStatus { get; set; } = "Pending";

        [StringLength(10)]
        public string? AuditedBy { get; set; }

        public DateTime? AuditedAt { get; set; }

        [StringLength(500)]
        public string? AuditRemarks { get; set; }

        // Navigation properties
        [ForeignKey("ScheduleID")]
        public virtual PaymentSchedule? PaymentSchedule { get; set; }

        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("AuditedBy")]
        public virtual User? AuditedByUser { get; set; }
    }
}
