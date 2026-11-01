using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        [StringLength(10)]
        public string? ProjectID { get; set; } = string.Empty;

        [StringLength(150)]
        public string? ProjectName { get; set; }

        [StringLength(7)]
        public string? Prefix { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(150)]
        public string? Location { get; set; }

        /// <summary>Comma-separated list of sizes (e.g. 5 Marla, 7 Marla, 10 Marla).</summary>
        [StringLength(1000)]
        public string? Sizes { get; set; }

        /// <summary>Comma-separated list of sub-projects (e.g. Phase 1, Block A).</summary>
        [StringLength(1000)]
        public string? SubProjects { get; set; }

        /// <summary>Comma-separated list of property types (e.g. Plot, Apartment, House).</summary>
        [StringLength(500)]
        public string? PropertyTypes { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<PaymentPlan> PaymentPlans { get; set; } = new List<PaymentPlan>();
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
        public virtual ICollection<Balloting> Ballotings { get; set; } = new List<Balloting>();
    }
}
