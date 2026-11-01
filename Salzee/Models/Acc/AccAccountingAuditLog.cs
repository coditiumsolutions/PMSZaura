using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("AccountingAuditLog", Schema = "acc")]
public class AccAccountingAuditLog
{
    public long LogID { get; set; }

    [StringLength(100)]
    public string TableName { get; set; } = string.Empty;

    [StringLength(50)]
    public string RecordID { get; set; } = string.Empty;

    [StringLength(20)]
    public string Action { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [StringLength(10)]
    public string ChangedBy { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }

    [StringLength(50)]
    public string? IPAddress { get; set; }

    [StringLength(300)]
    public string? UserAgent { get; set; }
}
