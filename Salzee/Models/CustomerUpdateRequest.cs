using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("CustomerUpdateRequests")]
    public class CustomerUpdateRequest
    {
        [Key]
        [StringLength(10)]
        public string RequestID { get; set; } = string.Empty;

        [StringLength(10)]
        public string CustomerID { get; set; } = string.Empty;

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public string? ProposedDataJson { get; set; }
        public string? OriginalDataJson { get; set; }
        public string? ReviewerComments { get; set; }

        [StringLength(10)]
        public string? RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [StringLength(10)]
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(10)]
        public string? RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("RequestedBy")]
        public virtual User? RequestedByUser { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }

        [ForeignKey("RejectedBy")]
        public virtual User? RejectedByUser { get; set; }

        public virtual ICollection<CustomerUpdateRequestChange> Changes { get; set; } = new List<CustomerUpdateRequestChange>();
    }
}
