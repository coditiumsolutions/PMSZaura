using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Registration")]
    public class Registration
    {
        [Key]
        [StringLength(10)]
        public string? RegID { get; set; } = string.Empty;

        [StringLength(150)]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Full Name must contain letters only (spaces, dots, and hyphens are allowed).")]
        public string? FullName { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^\d{5}-\d{7}-\d$", ErrorMessage = "CNIC must be in format XXXXX-XXXXXXX-X (digits only).")]
        public string? CNIC { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Passport Number must be alphanumeric (letters and digits only).")]
        public string? PassportNo { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[0-9\+]+$", ErrorMessage = "Phone must contain digits and '+' only.")]
        public string? Phone { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FormNo { get; set; }

        [StringLength(10)]
        [Required(ErrorMessage = "Project is required.")]
        public string? ProjectID { get; set; }

        [StringLength(100)]
        [Required(ErrorMessage = "Size is required.")]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? SubProject { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? Status { get; set; } = "Pending";

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}
