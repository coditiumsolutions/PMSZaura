using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Configuration")]
    public class Configuration
    {
        [Key]
        [StringLength(50)]
        public string ConfigKey { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        public string? ConfigValue { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(10)]
        public string? UpdatedBy { get; set; }

        // Navigation property
        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }
    }
}

