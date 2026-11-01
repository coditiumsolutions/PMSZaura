using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("RefundCheques")]
    public class RefundCheque
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string RefundID { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ChequeNo { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime ChequeDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? Bank { get; set; }

        public string? Details { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(10)]
        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [StringLength(10)]
        [Column("modified_by")]
        public string? ModifiedBy { get; set; }

        [ForeignKey("RefundID")]
        public virtual Refund? Refund { get; set; }
    }
}
