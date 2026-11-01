using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("ACL")]
    public class ACL
    {
        [Key]
        [StringLength(10)]
        public string RoleID { get; set; } = string.Empty;

        [StringLength(100)]
        public string? RoleName { get; set; }

        public string? Permissions { get; set; }
    }
}
