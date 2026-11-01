using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [StringLength(10)]
        public string UserID { get; set; } = string.Empty;

        [StringLength(150)]
        public string? FullName { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(256)]
        public string? PasswordHash { get; set; }

        [StringLength(10)]
        public string? RoleID { get; set; }

        [StringLength(150)]
        public string? Designation { get; set; }

        [StringLength(150)]
        public string? Department { get; set; }

        [StringLength(50)]
        public string? UserType { get; set; }  // Admin, Manager, CRO

        public bool IsActive { get; set; } = true;

        /// <summary>When true, user must enter TOTP after password (or after completing setup if Enforce2FA).</summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>Data Protection–protected TOTP shared secret (Base32), never store raw secret.</summary>
        [StringLength(500)]
        public string? TwoFactorSecret { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RoleID")]
        public virtual ACL? Role { get; set; }

        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<UserModulePermission> ModulePermissions { get; set; } = new List<UserModulePermission>();
        public virtual ICollection<UserMacWhitelist> MacWhitelists { get; set; } = new List<UserMacWhitelist>();
        public virtual ICollection<BlockedMacLoginAttempt> BlockedMacLoginAttempts { get; set; } = new List<BlockedMacLoginAttempt>();
    }
}
