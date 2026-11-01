using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Balloting")]
    public class Balloting
    {
        [Key]
        [StringLength(10)]
        public string BallotID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ProjectID { get; set; }

        [StringLength(10)]
        public string? ConductedBy { get; set; }

        public DateTime ConductedAt { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Remarks { get; set; }

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }

        [ForeignKey("ConductedBy")]
        public virtual User? ConductedByUser { get; set; }
    }
}
