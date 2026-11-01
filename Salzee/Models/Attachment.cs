using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Attachments")]
    public class Attachment
    {
        [Key]
        [StringLength(10)]
        public string AttachmentID { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RefType { get; set; }

        [StringLength(100)]
        public string? RefID { get; set; }

        [StringLength(50)]
        public string? AttachmentType { get; set; } // 'CustomerPicture', 'IDCard', 'Other'

        [StringLength(255)]
        public string? FileName { get; set; }

        [StringLength(255)]
        public string? FilePath { get; set; }

        public long? FileSize { get; set; }

        [StringLength(100)]
        public string? FileType { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(10)]
        public string? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UploadedBy")]
        public virtual User? UploadedByUser { get; set; }
    }
}
