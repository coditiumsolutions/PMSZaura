using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("ProjectSubProjects")]
    public class ProjectSubProject
    {
        [Key]
        [StringLength(10)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ProjectID { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string SubProjectName { get; set; } = string.Empty;

        [Required]
        [StringLength(7)]
        public string Prefix { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }
    }
}

