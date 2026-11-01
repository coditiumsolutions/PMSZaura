using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("UserMacWhitelist")]
    public class UserMacWhitelist
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

        [StringLength(10)]
        public string? AddedBy { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
