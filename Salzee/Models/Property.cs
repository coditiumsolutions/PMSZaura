using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Property")]
    public class Property
    {
        [Key]
        [StringLength(10)]
        public string PropertyID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ProjectID { get; set; }

        [StringLength(100)]
        public string? SubProject { get; set; }

        [StringLength(50)]
        public string? PlotNo { get; set; }

        [StringLength(50)]
        public string? Street { get; set; }

        [StringLength(50)]
        public string? PlotType { get; set; }

        [StringLength(50)]
        public string? Block { get; set; }

        [StringLength(50)]
        public string? Floor { get; set; }

        [StringLength(50)]
        public string? PropertyType { get; set; }

        [StringLength(50)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Available";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? AdditionalInfo { get; set; }

        public int? DealerID { get; set; }

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }

        [ForeignKey("DealerID")]
        public virtual Dealer? Dealer { get; set; }

        public virtual ICollection<Allotment> Allotments { get; set; } = new List<Allotment>();
        public virtual ICollection<Possession> Possessions { get; set; } = new List<Possession>();
        public virtual ICollection<PropertyLog> PropertyLogs { get; set; } = new List<PropertyLog>();
        public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    }
}
