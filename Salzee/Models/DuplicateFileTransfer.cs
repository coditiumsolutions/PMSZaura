using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    /// <summary>Duplicate file transfer request (workflow only; does not mutate customer or payments on approve/decline).</summary>
    [Table("DuplicateFileTransfer")]
    public class DuplicateFileTransfer
    {
        public const string TransferFeeTypeName = "Duplicate File Transfer";

        [Key]
        [StringLength(10)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string CustomerID { get; set; } = string.Empty;

        [StringLength(150)]
        public string? CustomerName { get; set; }

        [StringLength(50)]
        public string? CustomerCNIC { get; set; }

        [Column("Created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("Created_by")]
        [StringLength(10)]
        public string? CreatedBy { get; set; }

        [Column("Modified_by")]
        [StringLength(10)]
        public string? ModifiedBy { get; set; }

        /// <summary>Initiated | Approved | Declined</summary>
        [StringLength(50)]
        public string? Status { get; set; } = "Initiated";

        public string? Comments { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FeeDue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FeePaid { get; set; }

        [StringLength(100)]
        public string? ChallanID { get; set; }

        [StringLength(150)]
        public string? BankName { get; set; }

        [StringLength(100)]
        public string? InstrumentNo { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DepositDate { get; set; }

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public virtual Customer? Customer { get; set; }
    }
}
