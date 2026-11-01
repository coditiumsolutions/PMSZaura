using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("UserSessions")]
    public class UserSession
    {
        [Key]
        [StringLength(10)]
        public string SessionID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? UserID { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime? LogoutTime { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [StringLength(150)]
        public string? DeviceInfo { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
