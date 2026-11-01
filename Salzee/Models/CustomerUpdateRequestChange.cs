using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("CustomerUpdateRequestChanges")]
    public class CustomerUpdateRequestChange
    {
        [Key]
        [StringLength(10)]
        public string Id { get; set; } = string.Empty;

        [StringLength(10)]
        public string RequestID { get; set; } = string.Empty;

        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RequestID")]
        public virtual CustomerUpdateRequest? Request { get; set; }
    }
}
