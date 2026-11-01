using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Approvals")]
    public class Approval
    {
        [Key]
        [StringLength(10)]
        public string ApprovalID { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RefType { get; set; }

        [StringLength(10)]
        public string? RefID { get; set; }

        public string? ApprovedBy { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
