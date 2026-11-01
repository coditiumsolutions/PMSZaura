using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    /// <summary>
    /// Per-user, per-module access: NoAccess (no access), Read (view only), Edit (Create+Edit+Read), Admin (full CRUD).
    /// </summary>
    [Table("UserModulePermission")]
    public class UserModulePermission
    {
        [Key, Column(Order = 0)]
        [StringLength(10)]
        public string UserID { get; set; } = string.Empty;

        [Key, Column(Order = 1)]
        [StringLength(50)]
        public string ModuleKey { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Permission { get; set; } = "NoAccess"; // NoAccess, Read, Edit, Admin

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
