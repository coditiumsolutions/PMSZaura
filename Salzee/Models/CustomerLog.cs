using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("CustomerLogs")]
    public class CustomerLog
    {
        [Key]
        public int LogID { get; set; }

        [StringLength(10)]
        public string? CustomerID { get; set; }

        [StringLength(150)]
        public string? Action { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(10)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }
    }
}
