using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;
using System.Security.Claims;

namespace Salzee.Controllers
{
    [Authorize]
    public class AccountsManagementController : Controller
    {
        private const string ModuleKey = "AccountsManagement";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public AccountsManagementController(PMSDbContext context, IModulePermissionService modulePermission)
        {
            _context = context;
            _modulePermission = modulePermission;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
            if (requiredLevel == "Read" && !_modulePermission.CanRead(perm))
                return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Edit" && !_modulePermission.CanEdit(perm))
                return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Admin" && !await HttpContext.RequestServices.GetRequiredService<AmsAccessService>().IsAdminUserAsync(userId))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.CanCreate = _modulePermission.CanEdit(perm);
            ViewBag.CanEdit = _modulePermission.CanEdit(perm);
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var completedStatuses = new[] { "Completed", "Approved" };

            // Collections
            var totalCollectedToday = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= today && p.PaymentDate < today.AddDays(1))
                .Where(p => p.Status != null && completedStatuses.Contains(p.Status))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var totalCollectedThisMonth = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < nextMonthStart)
                .Where(p => p.Status != null && completedStatuses.Contains(p.Status))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var methodSummary = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < nextMonthStart)
                .Where(p => p.Status != null && completedStatuses.Contains(p.Status))
                .GroupBy(p => p.Method ?? "Unknown")
                .Select(g => new AccountsMethodSummaryItem
                {
                    Method = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            // Bank-wise collections (Bank Transfer only, infer bank from reference/remarks)
            var bankPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < nextMonthStart)
                .Where(p => p.Status != null && completedStatuses.Contains(p.Status))
                .Where(p => p.Method == "Bank Transfer")
                .Select(p => new { p.Amount, p.ReferenceNo, p.Remarks })
                .ToListAsync();

            var bankSummary = bankPayments
                .GroupBy(p => InferBank(p.ReferenceNo, p.Remarks))
                .Select(g => new AccountsBankSummaryItem
                {
                    Bank = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(10)
                .ToList();

            // Receivables this month (PaymentSchedule due in month - paid payments)
            var receivableItems = await _context.PaymentSchedules
                .AsNoTracking()
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Project)
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Customers)
                .Include(ps => ps.Payments)
                .Where(ps => ps.DueDate >= monthStart && ps.DueDate < nextMonthStart)
                .Select(ps => new
                {
                    ps.ScheduleID,
                    ps.DueDate,
                    ps.Amount,
                    ps.InstallmentNo,
                    ps.PaymentDescription,
                    PlanID = ps.PlanID,
                    PlanName = ps.PaymentPlan!.PlanName,
                    ProjectName = ps.PaymentPlan!.Project!.ProjectName,
                    Customer = ps.PaymentPlan!.Customers.OrderBy(c => c.CustomerID).Select(c => new { c.CustomerID, c.FullName }).FirstOrDefault(),
                    Paid = ps.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            var totalReceivableThisMonth = receivableItems.Sum(x => Math.Max(0m, x.Amount - x.Paid));

            // Overdue receivables (all schedules past due date)
            var overdueItems = await _context.PaymentSchedules
                .AsNoTracking()
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Customers)
                .Include(ps => ps.Payments)
                .Where(ps => ps.DueDate < today)
                .Select(ps => new
                {
                    DueDate = ps.DueDate,
                    Amount = ps.Amount,
                    Paid = ps.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            var totalOverdue = overdueItems.Sum(x => Math.Max(0m, x.Amount - x.Paid));
            var aging = BuildAgingBuckets(overdueItems.Select(x =>
            {
                var outstanding = Math.Max(0m, x.Amount - x.Paid);
                var days = (today - x.DueDate.Date).Days;
                return (days, outstanding);
            }).Where(x => x.outstanding > 0m).ToList());

            var vm = new AccountsDashboardViewModel
            {
                Today = today,
                PeriodStart = monthStart,
                PeriodEnd = nextMonthStart.AddDays(-1),
                TotalCollectedToday = totalCollectedToday,
                TotalCollectedThisMonth = totalCollectedThisMonth,
                TotalReceivableThisMonth = totalReceivableThisMonth,
                TotalOverdueReceivable = totalOverdue,
                CollectionsByMethodThisMonth = methodSummary,
                BankWiseCollectionsThisMonth = bankSummary,
                OverdueAgingSummary = aging
            };

            return View(vm);
        }

        public async Task<IActionResult> BankWiseCollections(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var today = DateTime.Today;
            var start = fromDate?.Date ?? new DateTime(today.Year, today.Month, 1);
            var end = (toDate?.Date ?? start.AddMonths(1).AddDays(-1));
            var endExclusive = end.AddDays(1);

            var completedStatuses = new[] { "Completed", "Approved" };

            var bankPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= start && p.PaymentDate < endExclusive)
                .Where(p => p.Status != null && completedStatuses.Contains(p.Status))
                .Where(p => p.Method == "Bank Transfer")
                .Select(p => new { p.Amount, p.ReferenceNo, p.Remarks })
                .ToListAsync();

            var banks = bankPayments
                .GroupBy(p => InferBank(p.ReferenceNo, p.Remarks))
                .Select(g => new AccountsBankSummaryItem
                {
                    Bank = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            var vm = new AccountsBankWiseCollectionsViewModel
            {
                FromDate = start,
                ToDate = end,
                TotalBankTransfer = bankPayments.Sum(x => x.Amount),
                Banks = banks
            };

            return View(vm);
        }

        public async Task<IActionResult> ReceivablesThisMonth()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var rows = await _context.PaymentSchedules
                .AsNoTracking()
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Project)
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Customers)
                .Include(ps => ps.Payments)
                .Where(ps => ps.DueDate >= monthStart && ps.DueDate < nextMonthStart)
                .Select(ps => new AccountsReceivableScheduleItem
                {
                    ScheduleID = ps.ScheduleID,
                    DueDate = ps.DueDate,
                    AmountDue = ps.Amount,
                    AmountPaid = ps.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
                    InstallmentNo = ps.InstallmentNo,
                    PaymentDescription = ps.PaymentDescription,
                    PlanID = ps.PlanID,
                    PlanName = ps.PaymentPlan!.PlanName,
                    ProjectName = ps.PaymentPlan!.Project!.ProjectName,
                    CustomerID = ps.PaymentPlan!.Customers.OrderBy(c => c.CustomerID).Select(c => c.CustomerID).FirstOrDefault(),
                    CustomerName = ps.PaymentPlan!.Customers.OrderBy(c => c.CustomerID).Select(c => c.FullName).FirstOrDefault(),
                    DaysPastDue = (today - ps.DueDate.Date).Days
                })
                .ToListAsync();

            var items = rows
                .Where(x => x.Outstanding > 0m)
                .OrderByDescending(x => x.Outstanding)
                .ThenBy(x => x.DueDate)
                .ToList();

            var vm = new AccountsReceivablesThisMonthViewModel
            {
                Year = today.Year,
                Month = today.Month,
                TotalDue = rows.Sum(x => x.AmountDue),
                TotalPaid = rows.Sum(x => x.AmountPaid),
                TotalOutstanding = rows.Sum(x => x.Outstanding),
                Items = items
            };

            return View(vm);
        }

        public async Task<IActionResult> OverdueAging()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var today = DateTime.Today;

            var rows = await _context.PaymentSchedules
                .AsNoTracking()
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Project)
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp!.Customers)
                .Include(ps => ps.Payments)
                .Where(ps => ps.DueDate < today)
                .Select(ps => new AccountsReceivableScheduleItem
                {
                    ScheduleID = ps.ScheduleID,
                    DueDate = ps.DueDate,
                    AmountDue = ps.Amount,
                    AmountPaid = ps.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
                    InstallmentNo = ps.InstallmentNo,
                    PaymentDescription = ps.PaymentDescription,
                    PlanID = ps.PlanID,
                    PlanName = ps.PaymentPlan!.PlanName,
                    ProjectName = ps.PaymentPlan!.Project!.ProjectName,
                    CustomerID = ps.PaymentPlan!.Customers.OrderBy(c => c.CustomerID).Select(c => c.CustomerID).FirstOrDefault(),
                    CustomerName = ps.PaymentPlan!.Customers.OrderBy(c => c.CustomerID).Select(c => c.FullName).FirstOrDefault(),
                    DaysPastDue = (today - ps.DueDate.Date).Days
                })
                .ToListAsync();

            var overdue = rows.Where(x => x.Outstanding > 0m).ToList();
            var buckets = BuildAgingBuckets(overdue.Select(x => (x.DaysPastDue, x.Outstanding)).ToList());

            var vm = new AccountsOverdueAgingViewModel
            {
                AsOfDate = today,
                TotalOutstanding = overdue.Sum(x => x.Outstanding),
                Buckets = buckets,
                TopOverdueItems = overdue
                    .OrderByDescending(x => x.Outstanding)
                    .ThenByDescending(x => x.DaysPastDue)
                    .Take(100)
                    .ToList()
            };

            return View(vm);
        }

        private static List<AccountsAgingBucketItem> BuildAgingBuckets(List<(int daysPastDue, decimal outstanding)> items)
        {
            int CountIn(Func<int, bool> predicate) => items.Count(x => predicate(x.daysPastDue) && x.outstanding > 0m);
            decimal SumIn(Func<int, bool> predicate) => items.Where(x => predicate(x.daysPastDue) && x.outstanding > 0m).Sum(x => x.outstanding);

            var buckets = new List<AccountsAgingBucketItem>
            {
                new() { Bucket = "1-30", Count = CountIn(d => d >= 1 && d <= 30), TotalOutstanding = SumIn(d => d >= 1 && d <= 30) },
                new() { Bucket = "31-60", Count = CountIn(d => d >= 31 && d <= 60), TotalOutstanding = SumIn(d => d >= 31 && d <= 60) },
                new() { Bucket = "61-90", Count = CountIn(d => d >= 61 && d <= 90), TotalOutstanding = SumIn(d => d >= 61 && d <= 90) },
                new() { Bucket = "90+", Count = CountIn(d => d >= 91), TotalOutstanding = SumIn(d => d >= 91) }
            };

            return buckets;
        }

        private static string InferBank(string? referenceNo, string? remarks)
        {
            var text = ((referenceNo ?? "") + " " + (remarks ?? "")).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(text))
                return "Unknown";

            // Common Pakistan banks (extend anytime; purely inference from existing free-text fields)
            var tokens = new[]
            {
                "HBL", "UBL", "MCB", "ABL", "NBP", "BAHL", "BOP", "SCB", "CITI", "JS", "FAYSAL", "MEZAN", "ALFALAH", "SILK", "ASKARI"
            };

            foreach (var t in tokens)
            {
                if (text.Contains(t))
                    return t;
            }

            return "Other/Unknown";
        }
    }
}

