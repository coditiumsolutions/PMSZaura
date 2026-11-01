using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("PropertyLogs")]
    public class PropertyLog
    {
        [Key]
        public int LogID { get; set; }

        [StringLength(10)]
        public string? PropertyID { get; set; }

        [StringLength(150)]
        public string? Action { get; set; }

        [StringLength(255)]
        public string? OldValue { get; set; }

        [StringLength(255)]
        public string? NewValue { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(10)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PropertyID")]
        public virtual Property? Property { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? User { get; set; }
    }
}

