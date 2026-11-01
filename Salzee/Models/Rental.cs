using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Salzee.Models
{
    [Table("Rental")]
    public class Rental : IValidatableObject
    {
        /// <summary>CNIC format: 5 digits, hyphen, 7 digits, hyphen, 1 digit (e.g. 11111-1111111-1).</summary>
        public static readonly Regex CnicRegex = new Regex(@"^\d{5}-\d{7}-\d$", RegexOptions.Compiled);

        public const string StatusActive = "Active";
        public const string StatusCompleted = "Completed";
        public const string StatusCancelled = "Cancelled";

        [Key]
        [StringLength(50)]
        public string RentalID { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PropertyID { get; set; } = string.Empty;

        // Tenant info (NOT linked to Customers table)
        [Required(ErrorMessage = "Tenant Name is required.")]
        [StringLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "CNIC is required.")]
        [StringLength(50)]
        public string? TenantCNIC { get; set; }

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(50)]
        public string? TenantPhone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(150)]
        public string? TenantEmail { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(255)]
        public string? TenantAddress { get; set; }

        // Rental terms
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Monthly Rent must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        [Required(ErrorMessage = "Security Deposit is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Security Deposit cannot be negative.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SecurityDeposit { get; set; }

        [Required(ErrorMessage = "Advance Rent is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Advance Rent cannot be negative.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdvanceRent { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [StringLength(10)]
        public string Currency { get; set; } = "PKR";

        [Required(ErrorMessage = "Start Date is required.")]
        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Duration (Months) is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 month.")]
        public int DurationMonths { get; set; } = 12;

        /// <summary>
        /// Day of month for rent due (1-28). Required.
        /// </summary>
        [Required(ErrorMessage = "Due Day (1-28) is required.")]
        [Range(1, 28, ErrorMessage = "Due Day must be between 1 and 28.")]
        public int? PaymentDueDayOfMonth { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = StatusActive;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("PropertyID")]
        public virtual Property? Property { get; set; }

        public virtual ICollection<RentalPayment> RentalPayments { get; set; } = new List<RentalPayment>();

        /// <summary>Validates CNIC format XXXXX-XXXXXXX-X (e.g. 11111-1111111-1).</summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var cnicTrimmed = TenantCNIC?.Trim() ?? "";
            if (!string.IsNullOrEmpty(cnicTrimmed) && !CnicRegex.IsMatch(cnicTrimmed))
                yield return new ValidationResult("CNIC must be in format XXXXX-XXXXXXX-X (5 digits, hyphen, 7 digits, hyphen, 1 digit).", new[] { nameof(TenantCNIC) });
        }
    }
}

