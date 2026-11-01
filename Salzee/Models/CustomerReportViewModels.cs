namespace Salzee.Models
{
    /// <summary>Allotted vs Not Allotted counts per project.</summary>
    public class AllottedPerProjectReportItem
    {
        public string? ProjectID { get; set; }
        public string? ProjectName { get; set; }
        public int Allotted { get; set; }
        public int NotAllotted { get; set; }
        public int Total => Allotted + NotAllotted;
    }

    /// <summary>Customers per project grouped by status.</summary>
    public class CustomersByStatusPerProjectItem
    {
        public string? ProjectID { get; set; }
        public string? ProjectName { get; set; }
        public string? Status { get; set; }
        public int Count { get; set; }
    }

    /// <summary>New customers count per month.</summary>
    public class NewCustomersPerMonthItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string YearMonthLabel { get; set; } = "";
        public int Count { get; set; }
    }

    /// <summary>Customers count by city.</summary>
    public class CustomersByCityItem
    {
        public string? City { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Customers count by registered size (overall).</summary>
    public class CustomersBySizeItem
    {
        public string? Size { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Summary totals for dashboard.</summary>
    public class CustomerReportsSummary
    {
        public int TotalCustomers { get; set; }
        public int TotalAllotted { get; set; }
        public int TotalNotAllotted { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
    }
}
