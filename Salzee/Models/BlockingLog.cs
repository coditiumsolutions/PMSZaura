using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    /// <summary>Log of customer status change (block/unblock) with reason and optional attachment. Also logged to ActivityLog.</summary>
    [Table("BlockingLogs")]
    public class BlockingLog
    {
        [Key]
        public int BlockingLogID { get; set; }

        [Required]
        [StringLength(10)]
        public string CustomerID { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string UserID { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? PreviousStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Optional path to uploaded file (e.g. under wwwroot/uploads/blocking/).</summary>
        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
