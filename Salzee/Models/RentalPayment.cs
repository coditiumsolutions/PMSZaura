using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("RentalPayments")]
    public class RentalPayment
    {
        public const string StatusPending = "Pending";
        public const string StatusPaid = "Paid";
        public const string StatusPartiallyPaid = "Partially Paid";
        public const string StatusWaived = "Waived";

        [Key]
        [StringLength(50)]
        public string RentalPaymentID { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RentalID { get; set; } = string.Empty;

        public int BillingYear { get; set; }
        public int BillingMonth { get; set; } // 1-12

        [Column(TypeName = "date")]
        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        public DateTime? PaidOn { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        public string? ReferenceNo { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = StatusPending;

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("RentalID")]
        public virtual Rental? Rental { get; set; }
    }
}

