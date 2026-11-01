using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Transfer")]
    public class Transfer
    {
        [Key]
        [StringLength(50)]
        public string TransferID { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string CustomerID { get; set; } = string.Empty;

        [StringLength(100)]
        public string? WorkFlowStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Seller (current owner at initiation)
        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Seller Name must contain letters only.")]
        public string? SellerName { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Seller Father Name must contain letters only.")]
        public string? SellerFatherName { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^\d{5}-\d{7}-\d$", ErrorMessage = "Seller CNIC must be in format XXXXX-XXXXXXX-X (digits only).")]
        public string? SellerCNIC { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^[0-9\+]+$", ErrorMessage = "Seller Contact must contain digits and '+' only.")]
        public string? SellerContact { get; set; }

        [StringLength(200)]
        public string? SellerAddress { get; set; }

        // Buyer (new owner)
        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Buyer Name must contain letters only.")]
        public string? BuyerName { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Buyer Father Name must contain letters only.")]
        public string? BuyerFatherName { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^\d{5}-\d{7}-\d$", ErrorMessage = "CNIC must be in format XXXXX-XXXXXXX-X (digits only).")]
        public string? BuyerCNIC { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Passport Number must be alphanumeric.")]
        public string? BuyerPassportNo { get; set; }

        public DateTime? BuyerDOB { get; set; }

        [StringLength(20)]
        public string? BuyerGender { get; set; }

        [StringLength(100)]
        public string? BuyerNationality { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Email Address format is invalid.")]
        public string? BuyerEmail { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[0-9\+]+$", ErrorMessage = "Phone must contain digits and '+' only.")]
        public string? BuyerPhone { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Mobile must contain digits only.")]
        public string? BuyerMobile { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Mobile 2 must contain digits only.")]
        public string? BuyerMobile2 { get; set; }

        [StringLength(200)]
        [RegularExpression(@"^[0-9\+]+$", ErrorMessage = "Contact must contain digits and '+' only.")]
        public string? BuyerContact { get; set; }

        [StringLength(200)]
        public string? BuyerAddress { get; set; }

        [StringLength(255)]
        public string? BuyerMailingAddress { get; set; }

        [StringLength(255)]
        public string? BuyerPermanentAddress { get; set; }

        [StringLength(200)]
        public string? BuyerCity { get; set; }

        [StringLength(200)]
        public string? BuyerCountry { get; set; }

        /// <summary>JSON array of attachment refs</summary>
        public string? BuyerAttachments { get; set; }

        /// <summary>JSON array of attachment refs</summary>
        public string? SellerAttachments { get; set; }

        /// <summary>Fingerprint/biometric template data (e.g. base64) for seller. Stored as NVARCHAR(MAX).</summary>
        public string? SellerBiometric { get; set; }

        /// <summary>Fingerprint/biometric template data (e.g. base64) for buyer. Stored as NVARCHAR(MAX).</summary>
        public string? BuyerBiometric { get; set; }

        public double? TransferFeeDue { get; set; }
        public double? TransferFeePaid { get; set; }
        public DateTime? PaymentDate { get; set; }

        [StringLength(200)]
        public string? PaymentMode { get; set; }

        /// <summary>Instrument or channel detail (e.g. cheque, online ref); distinct from PaymentMode when needed.</summary>
        [StringLength(200)]
        public string? PaymentMethod { get; set; }

        [StringLength(200)]
        public string? PaymentChallanNo { get; set; }

        [StringLength(200)]
        public string? BankName { get; set; }

        /// <summary>Free-text payment notes for this transfer fee payment.</summary>
        public string? PaymentDetails { get; set; }

        public string? Details { get; set; }
        public string? CROComments { get; set; }
        public string? AccountsComments { get; set; }
        public string? TransferComments { get; set; }

        // Navigation
        [ForeignKey("CustomerID")]
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<TransferJointOwner> TransferJointOwners { get; set; } = new List<TransferJointOwner>();
    }
}
