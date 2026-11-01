using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Penalties")]
    public class Penalty
    {
        [Key]
        [StringLength(10)]
        public string PenaltyID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CustomerID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(255)]
        [Column("PenaltyReason")]
        public string? Reason { get; set; }

        [Column("PenaltyDate")]
        public DateTime AppliedOn { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }
    }
}
