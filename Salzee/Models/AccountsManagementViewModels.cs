using System.ComponentModel.DataAnnotations;

namespace Salzee.Models
{
    public class AccountsDashboardViewModel
    {
        public DateTime Today { get; set; } = DateTime.Today;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public decimal TotalCollectedToday { get; set; }
        public decimal TotalCollectedThisMonth { get; set; }

        public decimal TotalReceivableThisMonth { get; set; }
        public decimal TotalOverdueReceivable { get; set; }

        public List<AccountsMethodSummaryItem> CollectionsByMethodThisMonth { get; set; } = new();
        public List<AccountsBankSummaryItem> BankWiseCollectionsThisMonth { get; set; } = new();
        public List<AccountsAgingBucketItem> OverdueAgingSummary { get; set; } = new();
    }

    public class AccountsBankWiseCollectionsViewModel
    {
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        public decimal TotalBankTransfer { get; set; }
        public List<AccountsBankSummaryItem> Banks { get; set; } = new();
    }

    public class AccountsReceivablesThisMonthViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }

        public List<AccountsReceivableScheduleItem> Items { get; set; } = new();
    }

    public class AccountsOverdueAgingViewModel
    {
        public DateTime AsOfDate { get; set; } = DateTime.Today;
        public decimal TotalOutstanding { get; set; }
        public List<AccountsAgingBucketItem> Buckets { get; set; } = new();
        public List<AccountsReceivableScheduleItem> TopOverdueItems { get; set; } = new();
    }

    public class AccountsMethodSummaryItem
    {
        public string Method { get; set; } = "—";
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class AccountsBankSummaryItem
    {
        public string Bank { get; set; } = "Unknown";
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class AccountsAgingBucketItem
    {
        public string Bucket { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalOutstanding { get; set; }
    }

    public class AccountsReceivableScheduleItem
    {
        public string ScheduleID { get; set; } = "";
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? PlanID { get; set; }
        public string? PlanName { get; set; }
        public string? ProjectName { get; set; }

        public int? InstallmentNo { get; set; }
        public string? PaymentDescription { get; set; }

        public DateTime DueDate { get; set; }

        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Outstanding => Math.Max(0m, AmountDue - AmountPaid);

        public int DaysPastDue { get; set; }
        public bool IsOverdue => DaysPastDue > 0;
    }
}

