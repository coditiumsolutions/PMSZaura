using System.ComponentModel.DataAnnotations;

namespace Salzee.Models
{
    public class ActivityLogIndexViewModel
    {
        // Filters
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public string? RefType { get; set; }
        public string? RefId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public string? Query { get; set; }

        public string? BtNo { get; set; }

        public bool HasSearched { get; set; }

        // Paging
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        // Data
        public List<ActivityLogRow> Logs { get; set; } = new();

        // Dropdown options
        public List<UserOption> Users { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public List<string> RefTypes { get; set; } = new();
    }

    public class ActivityLogRow
    {
        public int LogId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public string UserName { get; set; } = "Unknown";
        public string? Action { get; set; }
        public string? RefType { get; set; }
        public string? RefId { get; set; }
        public string? Details { get; set; }
    }

    public class UserOption
    {
        public string UserId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}

