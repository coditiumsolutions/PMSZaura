using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("PropertyInquiry")]
    public class PropertyInquiry
    {
        [Key]
        public int InquiryID { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress]
        public string? EmailAddress { get; set; }

        [StringLength(100)]
        public string? InquiryType { get; set; }

        public string? Message { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } = "New";

        [StringLength(100)]
        public string? AssignedTo { get; set; }

        public DateTime? FollowUpDate { get; set; }

        public string? Notes { get; set; }

        public bool IsContacted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

