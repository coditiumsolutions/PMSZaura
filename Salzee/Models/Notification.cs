using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [StringLength(10)]
        public string NotificationID { get; set; } = string.Empty;

        [StringLength(10)]
        public string? UserID { get; set; }

        [StringLength(255)]
        public string? Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
