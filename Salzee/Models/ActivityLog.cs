using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("ActivityLog")]
    public class ActivityLog
    {
        [Key]
        public int LogID { get; set; }

        [StringLength(10)]
        public string? UserID { get; set; }

        [StringLength(255)]
        public string? Action { get; set; }

        [StringLength(50)]
        public string? RefType { get; set; }

        [StringLength(100)]
        public string? RefID { get; set; }

        /// <summary>Optional structured audit payload (JSON) for compliance and investigations.</summary>
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
