using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("BlockedMacLoginAttempt")]
    public class BlockedMacLoginAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string UserID { get; set; } = string.Empty;

        [Required]
        [StringLength(128)]
        public string MacAddress { get; set; } = string.Empty; // mTLS client certificate thumbprint

        [StringLength(150)]
        public string? DeviceName { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public bool IsWhitelisted { get; set; } = false;
        public DateTime AttemptedAt { get; set; } = DateTime.Now;

        [StringLength(10)]
        public string? WhitelistedBy { get; set; }

        public DateTime? WhitelistedAt { get; set; }

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
