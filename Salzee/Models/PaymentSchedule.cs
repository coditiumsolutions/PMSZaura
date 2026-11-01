using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("PaymentSchedule")]
    public class PaymentSchedule
    {
        [Key]
        [StringLength(10)]
        public string ScheduleID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? PlanID { get; set; }

        [StringLength(250)]
        public string? PaymentDescription { get; set; }

        public int? InstallmentNo { get; set; }

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountUSD { get; set; }

        public bool SurchargeApplied { get; set; } = true;

        [Column(TypeName = "decimal(18,6)")]
        public decimal SurchargeRate { get; set; } = 0.05m;

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("PlanID")]
        public virtual PaymentPlan? PaymentPlan { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
