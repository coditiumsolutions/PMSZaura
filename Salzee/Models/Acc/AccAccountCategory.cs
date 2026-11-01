using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models.Acc;

[Table("AccountCategory", Schema = "acc")]
public class AccAccountCategory
{
    public int AccountCategoryID { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(20)]
    public string NatureType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<AccAccountHead> AccountHeads { get; set; } = new List<AccAccountHead>();
}
