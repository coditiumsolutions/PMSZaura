using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("TransferFee")]
    public class TransferFee
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "char(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Column(TypeName = "char(10)")]
        public string ProjectID { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SubProject { get; set; }

        [StringLength(100)]
        public string? TransferType { get; set; }

        [StringLength(20)]
        public string? TransferPriority { get; set; } // Normal / Urgent

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPerUnit { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(10)]
        [Column(TypeName = "char(10)")]
        public string? CreatedBy { get; set; }

        [StringLength(10)]
        [Column(TypeName = "char(10)")]
        public string? ModifiedBy { get; set; }

        public string? Details { get; set; }

        [ForeignKey(nameof(ProjectID))]
        public virtual Project? Project { get; set; }
    }
}
