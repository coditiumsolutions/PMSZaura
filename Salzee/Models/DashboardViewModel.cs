using Salzee.Models;

namespace Salzee.Models
{
    public class DashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int PendingCustomers { get; set; }
        public int PendingCustomerUpdateRequests { get; set; }
        public int PendingWaivers { get; set; }
        public int PendingTransfers { get; set; }
        public int PendingRefunds { get; set; }
        public int PendingAllotments { get; set; }
        public string PendingWaiverStatusFilter { get; set; } = "Initiated";
        public bool ShowPendingTasks { get; set; }
        public int TotalProjects { get; set; }
        public int TotalProperties { get; set; }
        public int AvailableProperties { get; set; }
        public int AllottedProperties { get; set; }
        public decimal TotalPayments { get; set; }
        public List<Customer> RecentCustomers { get; set; } = new List<Customer>();
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
        
        // Chart Data
        public Dictionary<string, int> PropertyStatusData { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> MonthlyPaymentsData { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, int> PaymentStatusData { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CustomerTrendData { get; set; } = new Dictionary<string, int>();
        
        // Dealer Data
        public List<DealerDashboardData> DealerData { get; set; } = new List<DealerDashboardData>();
    }

    public class DealerDashboardData
    {
        public string DealershipName { get; set; } = string.Empty;
        public int Customers { get; set; }
        public int Properties { get; set; }
    }
}
