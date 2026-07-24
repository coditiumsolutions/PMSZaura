using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace PMS.Models
{
    [Table("Customers")]
    public class Customer : IValidatableObject
    {
        [Key]
        [StringLength(10)]
        public string? CustomerID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? RegID { get; set; }

        [Required(ErrorMessage = "Payment Plan Required")]
        [StringLength(10)]
        public string? PlanID { get; set; }

        [Required]
        [StringLength(10)]
        public string? ProjectID { get; set; }

        // Personal Info
        [StringLength(150)]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Full Name must contain letters only (spaces, dots, and hyphens are allowed).")]
        public string? FullName { get; set; }

        [StringLength(150)]
        public string? FatherName { get; set; }

        [StringLength(50)]
        public string? CNIC { get; set; }

        [StringLength(50)]
        public string? PassportNo { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(100)]
        public string? Nationality { get; set; }

        // Contact Info (Phone & MobileNo required — format validated in IValidatableObject after trim)
        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(50)]
        public string? MobileNo { get; set; }

        [StringLength(50)]
        public string? MobileNo2 { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FormNo { get; set; }

        [Required(ErrorMessage = "Mailing Address is required.")]
        [StringLength(255)]
        public string? MailingAddress { get; set; }

        [StringLength(255)]
        public string? PermanentAddress { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string? City { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string? Country { get; set; }

        // Project Info
        [Required]
        [StringLength(100)]
        public string? SubProject { get; set; }

        [Required]
        [StringLength(50)]
        public string? RegisteredSize { get; set; }

        // System Info
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? Status { get; set; } = "Pending";

        // Nominee Info
        [StringLength(100)]
        public string? NomineeName { get; set; }

        [StringLength(50)]
        public string? NomineeID { get; set; }

        [StringLength(50)]
        public string? NomineeRelation { get; set; }

        [StringLength(255)]
        public string? NomineeNICDocumentPath { get; set; }

        [StringLength(255)]
        public string? NomineePicturePath { get; set; }

        public string? AdditionalInfo { get; set; }

        /// <summary>Optional. Customer can exist without a dealer.</summary>
        public int? DealerID { get; set; }

        /// <summary>1=Yes (use DealerID), 0=No (use DealerName).</summary>
        [Column("isDealerRegistered")]
        public int? IsDealerRegistered { get; set; }

        /// <summary>Manual dealer name when IsDealerRegistered=0.</summary>
        [StringLength(200)]
        public string? DealerName { get; set; }

        // Navigation properties
        [ForeignKey("RegID")]
        public virtual Registration? Registration { get; set; }

        [ForeignKey("DealerID")]
        public virtual Dealer? Dealer { get; set; }

        [ForeignKey("PlanID")]
        public virtual PaymentPlan? PaymentPlan { get; set; }

        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }

        public virtual ICollection<CustomerLog> CustomerLogs { get; set; } = new List<CustomerLog>();
        public virtual ICollection<Allotment> Allotments { get; set; } = new List<Allotment>();
        public virtual ICollection<Possession> Possessions { get; set; } = new List<Possession>();
        public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();
        public virtual ICollection<Waiver> Waivers { get; set; } = new List<Waiver>();
        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
        public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
        public virtual ICollection<NDC> NDCs { get; set; } = new List<NDC>();
        public virtual ICollection<JointOwner> JointOwners { get; set; } = new List<JointOwner>();
        public virtual ICollection<TransferJointOwner> TransferJointOwners { get; set; } = new List<TransferJointOwner>();
        public virtual ICollection<DuplicateFileTransfer> DuplicateFileTransfers { get; set; } = new List<DuplicateFileTransfer>();
        public virtual ICollection<CustomerUpdateRequest> CustomerUpdateRequests { get; set; } = new List<CustomerUpdateRequest>();

        /// <summary>Either CNIC format 5-7-1 (e.g. 11111-1111111-1) or Passport No with at least 5 characters.</summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            Phone = Phone?.Trim() ?? "";
            MobileNo = MobileNo?.Trim() ?? "";
            MobileNo2 = string.IsNullOrWhiteSpace(MobileNo2) ? null : MobileNo2.Trim();

            if (string.IsNullOrWhiteSpace(Phone))
                yield return new ValidationResult("Phone is required.", new[] { nameof(Phone) });
            else if (!Regex.IsMatch(Phone, @"^[0-9\+]+$"))
                yield return new ValidationResult("Phone must contain digits and '+' only.", new[] { nameof(Phone) });

            if (string.IsNullOrWhiteSpace(MobileNo))
            {
                yield return new ValidationResult("Mobile number is required.", new[] { nameof(MobileNo) });
            }
            else
            {
                var mobileDigits = NormalizePhoneDigits(MobileNo);
                if (!Regex.IsMatch(MobileNo, @"^[0-9]+$") || mobileDigits.Length < 11 || mobileDigits.Length > 13)
                    yield return new ValidationResult("Mobile must contain digits only and be 11 to 13 digits long.", new[] { nameof(MobileNo) });
            }

            if (!string.IsNullOrEmpty(MobileNo2))
            {
                var mobile2Digits = NormalizePhoneDigits(MobileNo2);
                if (!Regex.IsMatch(MobileNo2, @"^[0-9]+$") || mobile2Digits.Length < 11 || mobile2Digits.Length > 13)
                    yield return new ValidationResult("Mobile 2 must contain digits only and be 11 to 13 digits long.", new[] { nameof(MobileNo2) });
            }

            var cnicTrimmed = CNIC?.Trim() ?? "";
            var cnicValid = Regex.IsMatch(cnicTrimmed, @"^\d{5}-\d{7}-\d$");
            if (!cnicValid)
                yield return new ValidationResult("National ID (CNIC) is required and must be in format XXXXX-XXXXXXX-X (5 digits, hyphen, 7 digits, hyphen, 1 digit).", new[] { nameof(CNIC) });
            // Dealer is optional on create/edit (Select Dealer / Dealer Name).
            // Phone, Mobile, and optional Mobile 2 must be different numbers (digits compared, ignoring formatting)
            var phoneNorm = NormalizePhoneDigits(Phone);
            var mobileNorm = NormalizePhoneDigits(MobileNo);
            var mobile2Norm = NormalizePhoneDigits(MobileNo2);
            if (!string.IsNullOrEmpty(phoneNorm) && !string.IsNullOrEmpty(mobileNorm) &&
                string.Equals(phoneNorm, mobileNorm, StringComparison.Ordinal))
            {
                yield return new ValidationResult("Phone and Mobile must be different numbers.", new[] { nameof(Phone), nameof(MobileNo) });
            }
            if (!string.IsNullOrEmpty(mobile2Norm))
            {
                if (!string.IsNullOrEmpty(phoneNorm) && string.Equals(phoneNorm, mobile2Norm, StringComparison.Ordinal))
                    yield return new ValidationResult("Mobile 2 must be a different number than Phone.", new[] { nameof(MobileNo2), nameof(Phone) });
                if (!string.IsNullOrEmpty(mobileNorm) && string.Equals(mobileNorm, mobile2Norm, StringComparison.Ordinal))
                    yield return new ValidationResult("Mobile 2 must be a different number than Mobile.", new[] { nameof(MobileNo2), nameof(MobileNo) });
            }
        }

        private static string NormalizePhoneDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return digits;
        }
    }
}
