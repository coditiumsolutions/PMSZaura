using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Possession")]
    public class Possession
    {
        [Key]
        [StringLength(40)]
        public string PossessionID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? PropertyID { get; set; }

        [StringLength(10)]
        public string? CustomerID { get; set; }

        public DateTime PossessionDate { get; set; } = DateTime.Now;

        /// <summary>Workflow from Configuration key possessionworkflow (e.g. Initiated, Operations Desk, Approved, Declined).</summary>
        [StringLength(100)]
        public string? WorkFlowStatus { get; set; }

        public string? Comments { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PossessionDueCharges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PossessionPaidCharges { get; set; }

        [StringLength(200)]
        public string? BankName { get; set; }

        public DateTime? PaidDate { get; set; }

        [StringLength(100)]
        public string? InstrumentNo { get; set; }

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        public DateTime? CreatedAt { get; set; }

        [StringLength(10)]
        public string? CreatedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }

        [StringLength(10)]
        public string? ModifiedBy { get; set; }

        [StringLength(10)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [StringLength(10)]
        public string? DeclinedBy { get; set; }

        public DateTime? DeclinedAt { get; set; }

        [ForeignKey("PropertyID")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("ModifiedBy")]
        public virtual User? ModifiedByUser { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }

        [ForeignKey("DeclinedBy")]
        public virtual User? DeclinedByUser { get; set; }
    }
}
