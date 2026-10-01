using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Models
{
    [Table("PaymentPlan")]
    public class PaymentPlan
    {
        [Key]
        [StringLength(10)]
        public string? PlanID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ProjectID { get; set; }

        [StringLength(100)]
        public string? RegisteredSize { get; set; }

        [StringLength(100)]
        public string? SubProject { get; set; }

        [StringLength(150)]
        public string? PlanName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmountUSD { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExchangeRate { get; set; }

        [StringLength(10)]
        public string? Currency { get; set; }

        public int? DurationMonths { get; set; }

        [StringLength(50)]
        public string? Frequency { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }

        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<PaymentSchedule> PaymentSchedules { get; set; } = new List<PaymentSchedule>();
    }
}
