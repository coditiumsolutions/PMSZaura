using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Dealers")]
    public class Dealer
    {
        [Key]
        public int DealerID { get; set; }

        [Required]
        [StringLength(500)]
        public string DealershipName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "date")]
        public DateTime RegisterationDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string MembershipType { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string OwnerName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string OwnerCNIC { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string MobileNo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string OwnerDetails { get; set; } = string.Empty;

        public string? Details { get; set; }

        [Column(TypeName = "float")]
        public double IncentivePercentage { get; set; } = 5.0;

        // Navigation properties
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}

