using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salzee.Models
{
    [Table("Tickets")]
    public class Ticket
    {
        [Key]
        [StringLength(10)]
        public string TicketID { get; set; } = string.Empty;

        [StringLength(150)]
        public string? CustomerID { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(256)]
        public string? Contact { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? CROComments { get; set; }

        [StringLength(256)]
        public string? Status { get; set; }

        [StringLength(256)]
        public string? CreatedBy { get; set; }

        [StringLength(256)]
        public string? AssignedTo { get; set; }

        public DateTime? TicketClosingDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
