using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Waiver")]
    public class Waiver
    {
        [Key]
        [StringLength(10)]
        public string WaiverID { get; set; } = string.Empty;

        [StringLength(255)]
        public string? WaiverType { get; set; } = "Surcharge Waiver";

        [StringLength(10)]
        public string? CustomerID { get; set; }

        [StringLength(100)]
        public string? Status { get; set; } = "Initiated";

        [StringLength(255)]
        public string? AccountHead { get; set; } = "Waived Off";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WaivedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WaivedPercentage { get; set; }

        public string? Comments { get; set; }

        [StringLength(10)]
        public string? ApprovedBy { get; set; }

        [StringLength(10)]
        public string? CreatedBy { get; set; }

        [StringLength(10)]
        public string? LastModifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("LastModifiedBy")]
        public virtual User? LastModifiedByUser { get; set; }
    }
}
