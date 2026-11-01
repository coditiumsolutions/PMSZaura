using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("TransferJointOwners")]
    public class TransferJointOwner
    {
        [Key]
        [StringLength(10)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TransferID { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string CustomerID { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string JointOwnerName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? FatherName { get; set; }

        [StringLength(50)]
        public string? CNIC { get; set; }

        [StringLength(50)]
        public string? Contact { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Percentage { get; set; }

        [Column("Created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("Created_by")]
        [StringLength(10)]
        public string? CreatedBy { get; set; }

        [Column("Modified_by")]
        [StringLength(10)]
        public string? ModifiedBy { get; set; }

        public string? Details { get; set; }

        [ForeignKey(nameof(TransferID))]
        public virtual Transfer? Transfer { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public virtual Customer? Customer { get; set; }
    }
}
