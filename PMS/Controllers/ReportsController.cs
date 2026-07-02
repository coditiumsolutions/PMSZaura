using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;

namespace PMS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private const string ModuleKey = "Reports";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private const string PendingStatus = "Pending";

        public ReportsController(PMSDbContext context, IModulePermissionService modulePermission)
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
            if (requiredLevel == "Admin" && !_modulePermission.CanDelete(perm))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.CanCreate = _modulePermission.CanEdit(perm);
            ViewBag.CanEdit = _modulePermission.CanEdit(perm);
            ViewBag.CanDelete = _modulePermission.CanDelete(perm);
            return null;
        }

        // 1. Defaulters Report - Customers with overdue payments
        public async Task<IActionResult> Defaulters()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var today = DateTime.Today;

            // Load overdue schedules without depending on Payments include,
            // so the page keeps working even if Payments table is missing.
            List<PaymentSchedule> defaulters;
            try
            {
                defaulters = await _context.PaymentSchedules
                    .AsNoTracking()
                    .Include(ps => ps.PaymentPlan)
                        .ThenInclude(pp => pp.Customers)
                    .Where(ps => ps.DueDate < today)
                    .ToListAsync();
            }
            catch
            {
                return View(new List<DefaulterReportViewModel>());
            }

            // Try to load payment totals separately (optional dependency).
            var paymentTotals = new Dictionary<string, decimal>();
            try
            {
                var paymentsTableExists = await _context.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Payments'")
                    .FirstOrDefaultAsync();

                if (paymentsTableExists > 0)
                {
                    paymentTotals = await _context.Payments
                        .AsNoTracking()
                        .Where(p => p.ScheduleID != null)
                        .GroupBy(p => p.ScheduleID!)
                        .Select(g => new { ScheduleID = g.Key, Total = g.Sum(p => (decimal?)p.Amount) ?? 0m })
                        .ToDictionaryAsync(x => x.ScheduleID, x => x.Total);
                }
            }
            catch
            {
                // Keep paymentTotals empty; overdue logic will treat paid amount as 0.
            }

            // Filter to only those without full payment
            var defaultersList = new List<DefaulterReportViewModel>();

            foreach (var schedule in defaulters)
            {
                var totalPaid = paymentTotals.TryGetValue(schedule.ScheduleID, out var paidAmount) ? paidAmount : 0m;
                var amountDue = schedule.Amount;
                var balance = amountDue - totalPaid;

                if (balance > 0) // Still has outstanding balance
                {
                    var customers = schedule.PaymentPlan?.Customers ?? new List<Customer>();
                    
                    foreach (var customer in customers)
                    {
                        if (string.Equals(customer.Status?.Trim(), PendingStatus, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        defaultersList.Add(new DefaulterReportViewModel
                        {
                            CustomerID = customer.CustomerID,
                            CustomerName = customer.FullName,
                            Phone = customer.Phone,
                            PlanName = schedule.PaymentPlan?.PlanName,
                            InstallmentNo = schedule.InstallmentNo,
                            DueDate = schedule.DueDate,
                            AmountDue = amountDue,
                            AmountPaid = totalPaid,
                            Balance = balance,
                            DaysOverdue = (today - schedule.DueDate).Days,
                            Status = customer.Status
                        });
                    }
                }
            }

            return View(defaultersList.OrderByDescending(d => d.DaysOverdue).ToList());
        }

        // 2. Not Allotted Report - Customers without property allotment
        public async Task<IActionResult> NotAllotted(string filter = "All")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            ViewBag.Filter = filter;

            // Get all customers
            var customers = await _context.Customers
                .Include(c => c.PaymentPlan)
                    .ThenInclude(pp => pp.PaymentSchedules)
                        .ThenInclude(ps => ps.Payments)
                .Where(c => !string.Equals(c.Status, PendingStatus))
                .ToListAsync();

            // Get customers with allotments
            var allottedCustomerIds = await _context.Allotments
                .Select(a => a.CustomerID)
                .Distinct()
                .ToListAsync();

            // Filter customers who are NOT allotted
            var notAllottedCustomers = customers
                .Where(c => !allottedCustomerIds.Contains(c.CustomerID))
                .ToList();

            var reportList = new List<NotAllottedReportViewModel>();

            foreach (var customer in notAllottedCustomers)
            {
                var paymentPlan = customer.PaymentPlan;
                var totalDue = paymentPlan?.TotalAmount ?? 0m;
                var totalPaid = paymentPlan?.PaymentSchedules?
                    .SelectMany(ps => ps.Payments ?? new List<Payment>())
                    .Where(p => p.CustomerID == customer.CustomerID)
                    .Sum(p => p.Amount) ?? 0m;

                var balance = totalDue - totalPaid;
                var paymentStatus = balance <= 0 ? "Fully Paid" : balance < totalDue ? "Partial" : "Defaulter";

                var model = new NotAllottedReportViewModel
                {
                    CustomerID = customer.CustomerID,
                    CustomerName = customer.FullName,
                    CNIC = customer.CNIC,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    PlanName = paymentPlan?.PlanName,
                    TotalAmount = totalDue,
                    TotalPaid = totalPaid,
                    Balance = balance,
                    PaymentStatus = paymentStatus,
                    CustomerStatus = customer.Status,
                    RegisteredDate = customer.CreatedAt
                };

                // Apply filter
                if (filter == "FullyPaid" && paymentStatus == "Fully Paid")
                {
                    reportList.Add(model);
                }
                else if (filter == "Defaulters" && paymentStatus == "Defaulter")
                {
                    reportList.Add(model);
                }
                else if (filter == "All")
                {
                    reportList.Add(model);
                }
            }

            return View(reportList.OrderBy(r => r.CustomerName).ToList());
        }

        // 3. Blocked Customers Report - Customers with inactive status
        public async Task<IActionResult> BlockedCustomers()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var blockedCustomers = await _context.Customers
                .Include(c => c.PaymentPlan)
                .Where(c => c.Status != "Active" && c.Status != PendingStatus)
                .OrderBy(c => c.FullName)
                .ToListAsync();

            var reportList = blockedCustomers.Select(c => new BlockedCustomerReportViewModel
            {
                CustomerID = c.CustomerID,
                CustomerName = c.FullName,
                CNIC = c.CNIC,
                Phone = c.Phone,
                Email = c.Email,
                Status = c.Status,
                PlanName = c.PaymentPlan?.PlanName,
                RegisteredDate = c.CreatedAt,
                City = c.City,
                Country = c.Country
            }).ToList();

            return View(reportList);
        }

        // 4. Property Report - Allotted vs Not Allotted by Project, Size, Block
        public async Task<IActionResult> PropertyReport(string projectId = null)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            ViewBag.Projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
            ViewBag.SelectedProjectId = projectId;

            var query = _context.Properties
                .Include(p => p.Project)
                .Include(p => p.Allotments)
                    .ThenInclude(a => a.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(projectId))
            {
                query = query.Where(p => p.ProjectID == projectId);
            }

            var properties = await query.OrderBy(p => p.Project.ProjectName)
                .ThenBy(p => p.Block)
                .ThenBy(p => p.PlotNo)
                .ToListAsync();

            var reportList = properties.Select(p => new PropertyReportViewModel
            {
                AllottedDate = p.Allotments?
                    .Where(a => a.Customer != null && !string.Equals(a.Customer.Status, PendingStatus))
                    .OrderBy(a => a.AllotmentDate)
                    .Select(a => (DateTime?)a.AllotmentDate)
                    .FirstOrDefault(),
                AllottedTo = p.Allotments?
                    .Where(a => a.Customer != null && !string.Equals(a.Customer.Status, PendingStatus))
                    .OrderBy(a => a.AllotmentDate)
                    .Select(a => a.Customer!.FullName)
                    .FirstOrDefault(),
                PropertyID = p.PropertyID,
                ProjectName = p.Project?.ProjectName,
                PlotNo = p.PlotNo,
                Block = p.Block,
                Size = p.Size,
                PropertyType = p.PropertyType,
                Status = p.Status,
                IsAllotted = p.Allotments != null && p.Allotments.Any(a => a.Customer != null && !string.Equals(a.Customer.Status, PendingStatus)),
                Street = p.Street
            }).ToList();

            // Summary Statistics
            ViewBag.TotalProperties = reportList.Count;
            ViewBag.AllottedCount = reportList.Count(p => p.IsAllotted);
            ViewBag.NotAllottedCount = reportList.Count(p => !p.IsAllotted);

            return View(reportList);
        }

        // Requested reports placeholder - used for sidebar report links that are planned.
        public async Task<IActionResult> RequestedReport(string id = "", string title = "", int x = 2, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var reportId = (id ?? string.Empty).Trim().ToLowerInvariant();
            ViewBag.ReportId = reportId;
            ViewBag.ReportTitle = string.IsNullOrWhiteSpace(title) ? "Requested Report" : title.Trim();
            ViewBag.ReportColumns = new List<string>();
            ViewBag.ReportRows = new List<List<string>>();
            ViewBag.ReportSummary = new Dictionary<string, string>();
            ViewBag.ReportDescription = "No data.";
            ViewBag.FilterX = x < 1 ? 1 : x;
            ViewBag.FromDate = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
            ViewBag.ToDate = (toDate ?? DateTime.Today).Date;

            switch (reportId)
            {
                case "total-active-members":
                {
                    var activeCustomers = await _context.Customers
                        .AsNoTracking()
                        .Include(c => c.PaymentPlan)
                        .Where(c => string.Equals(c.Status, "Active"))
                        .OrderBy(c => c.FullName)
                        .ToListAsync();

                    ViewBag.ReportDescription = "All active customers.";
                    ViewBag.ReportColumns = new List<string> { "Customer ID", "Name", "CNIC", "Phone", "Plan", "Created" };
                    ViewBag.ReportRows = activeCustomers.Select(c => new List<string>
                    {
                        c.CustomerID ?? "—",
                        c.FullName ?? "—",
                        c.CNIC ?? "—",
                        c.Phone ?? "—",
                        c.PaymentPlan?.PlanName ?? "—",
                        c.CreatedAt.ToString("MMM dd, yyyy")
                    }).ToList();
                    break;
                }
                case "defaulter-x-installments":
                {
                    var threshold = x < 1 ? 1 : x;
                    var overdueSchedules = await _context.PaymentSchedules
                        .AsNoTracking()
                        .Include(ps => ps.PaymentPlan)
                            .ThenInclude(pp => pp.Customers)
                        .Include(ps => ps.Payments)
                        .Where(ps => ps.DueDate < DateTime.Today)
                        .ToListAsync();

                    var defaulterRows = new List<(Customer c, int Count, decimal Outstanding)>();
                    var customerMap = new Dictionary<string, (Customer c, int Count, decimal Outstanding)>();

                    foreach (var schedule in overdueSchedules)
                    {
                        var paid = schedule.Payments?.Sum(p => p.Amount) ?? 0m;
                        var outstanding = Math.Max(schedule.Amount - paid, 0m);
                        if (outstanding <= 0m) continue;

                        var customers = schedule.PaymentPlan?.Customers ?? new List<Customer>();
                        foreach (var customer in customers)
                        {
                            if (customer.CustomerID == null) continue;
                            if (string.Equals(customer.Status, PendingStatus, StringComparison.OrdinalIgnoreCase)) continue;

                            if (!customerMap.TryGetValue(customer.CustomerID, out var existing))
                                customerMap[customer.CustomerID] = (customer, 1, outstanding);
                            else
                                customerMap[customer.CustomerID] = (existing.c, existing.Count + 1, existing.Outstanding + outstanding);
                        }
                    }

                    defaulterRows = customerMap.Values
                        .Where(v => v.Count >= threshold)
                        .OrderByDescending(v => v.Count)
                        .ThenByDescending(v => v.Outstanding)
                        .ToList();

                    ViewBag.ReportDescription = $"Customers with at least {threshold} overdue installment(s).";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["Threshold (X)"] = threshold.ToString(),
                        ["Customers Found"] = defaulterRows.Count.ToString("N0"),
                        ["Total Outstanding"] = defaulterRows.Sum(v => v.Outstanding).ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Customer ID", "Name", "Phone", "Status", "Overdue Installments", "Outstanding (PKR)" };
                    ViewBag.ReportRows = defaulterRows.Select(v => new List<string>
                    {
                        v.c.CustomerID ?? "—",
                        v.c.FullName ?? "—",
                        v.c.Phone ?? "—",
                        v.c.Status ?? "—",
                        v.Count.ToString("N0"),
                        v.Outstanding.ToString("N0")
                    }).ToList();
                    break;
                }
                case "all-paid-customers":
                {
                    var customers = await _context.Customers
                        .AsNoTracking()
                        .Include(c => c.PaymentPlan)
                        .Where(c => c.PlanID != null && !string.Equals(c.Status, PendingStatus))
                        .ToListAsync();

                    var paidCustomers = new List<(Customer c, decimal Paid, decimal Due)>();
                    foreach (var c in customers)
                    {
                        var due = c.PaymentPlan?.TotalAmount ?? 0m;
                        var paid = await _context.Payments
                            .AsNoTracking()
                            .Where(p => p.CustomerID == c.CustomerID)
                            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
                        if (due > 0 && paid >= due)
                            paidCustomers.Add((c, paid, due));
                    }

                    ViewBag.ReportDescription = "Customers whose total paid amount is equal to or above plan total amount.";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["All Paid Customers"] = paidCustomers.Count.ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Customer ID", "Name", "Plan", "Plan Total", "Paid", "Excess" };
                    ViewBag.ReportRows = paidCustomers.Select(v => new List<string>
                    {
                        v.c.CustomerID ?? "—",
                        v.c.FullName ?? "—",
                        v.c.PaymentPlan?.PlanName ?? "—",
                        v.Due.ToString("N0"),
                        v.Paid.ToString("N0"),
                        Math.Max(v.Paid - v.Due, 0m).ToString("N0")
                    }).ToList();
                    break;
                }
                case "inst-due-amount":
                {
                    var schedules = await _context.PaymentSchedules
                        .AsNoTracking()
                        .Include(ps => ps.PaymentPlan)
                            .ThenInclude(pp => pp.Customers)
                        .Include(ps => ps.Payments)
                        .Where(ps => ps.DueDate <= DateTime.Today)
                        .OrderBy(ps => ps.DueDate)
                        .ToListAsync();

                    var rows = new List<List<string>>();
                    decimal totalOutstanding = 0m;
                    foreach (var sch in schedules)
                    {
                        var paid = sch.Payments?.Sum(p => p.Amount) ?? 0m;
                        var outstanding = Math.Max(sch.Amount - paid, 0m);
                        if (outstanding <= 0m) continue;
                        totalOutstanding += outstanding;
                        var customer = sch.PaymentPlan?.Customers?.FirstOrDefault();
                        rows.Add(new List<string>
                        {
                            customer?.CustomerID ?? "—",
                            customer?.FullName ?? "—",
                            sch.PaymentPlan?.PlanName ?? "—",
                            (sch.InstallmentNo?.ToString() ?? "—"),
                            sch.DueDate.ToString("MMM dd, yyyy"),
                            sch.Amount.ToString("N0"),
                            paid.ToString("N0"),
                            outstanding.ToString("N0")
                        });
                    }

                    ViewBag.ReportDescription = "Installments due till today with outstanding amounts.";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["Due Installments"] = rows.Count.ToString("N0"),
                        ["Total Outstanding"] = totalOutstanding.ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Customer ID", "Name", "Plan", "Inst #", "Due Date", "Due", "Paid", "Outstanding" };
                    ViewBag.ReportRows = rows;
                    break;
                }
                case "total-transfers-report":
                {
                    var transfers = await _context.Transfers
                        .AsNoTracking()
                        .Include(t => t.Customer)
                        .OrderByDescending(t => t.CreatedAt)
                        .ToListAsync();

                    ViewBag.ReportDescription = "All transfer records with fee status.";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["Total Transfers"] = transfers.Count.ToString("N0"),
                        ["Total Fee Received"] = transfers.Sum(t => (decimal)(t.TransferFeePaid ?? 0d)).ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Transfer ID", "Date", "Customer ID", "Customer", "Workflow", "Fee Due", "Fee Paid" };
                    ViewBag.ReportRows = transfers.Select(t => new List<string>
                    {
                        t.TransferID,
                        t.CreatedAt.ToString("MMM dd, yyyy"),
                        t.CustomerID,
                        t.Customer?.FullName ?? "—",
                        t.WorkFlowStatus ?? "—",
                        ((decimal)(t.TransferFeeDue ?? 0d)).ToString("N0"),
                        ((decimal)(t.TransferFeePaid ?? 0d)).ToString("N0")
                    }).ToList();
                    break;
                }
                case "daily-transfer-report":
                {
                    var from = ((DateTime)ViewBag.FromDate).Date;
                    var to = ((DateTime)ViewBag.ToDate).Date;
                    if (to < from) (from, to) = (to, from);

                    var daily = await _context.Transfers
                        .AsNoTracking()
                        .Where(t => t.CreatedAt.Date >= from && t.CreatedAt.Date <= to)
                        .GroupBy(t => t.CreatedAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Count = g.Count(),
                            FeePaid = g.Sum(x => (decimal?)(x.TransferFeePaid ?? 0d)) ?? 0m
                        })
                        .OrderBy(g => g.Date)
                        .ToListAsync();

                    ViewBag.ReportDescription = $"Daily transfer activity from {from:MMM dd, yyyy} to {to:MMM dd, yyyy}.";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["Days with Activity"] = daily.Count.ToString("N0"),
                        ["Transfers"] = daily.Sum(x => x.Count).ToString("N0"),
                        ["Fee Received"] = daily.Sum(x => x.FeePaid).ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Date", "Transfers", "Fee Received (PKR)" };
                    ViewBag.ReportRows = daily.Select(d => new List<string>
                    {
                        d.Date.ToString("MMM dd, yyyy"),
                        d.Count.ToString("N0"),
                        d.FeePaid.ToString("N0")
                    }).ToList();
                    break;
                }
                case "project-wise-transfer":
                {
                    var transfers = await _context.Transfers
                        .AsNoTracking()
                        .Include(t => t.Customer)
                            .ThenInclude(c => c!.PaymentPlan)
                                .ThenInclude(pp => pp!.Project)
                        .ToListAsync();

                    var grouped = transfers
                        .GroupBy(t => t.Customer?.PaymentPlan?.Project?.ProjectName ?? "Unknown")
                        .Select(g => new
                        {
                            Project = g.Key,
                            Count = g.Count(),
                            FeePaid = g.Sum(x => (decimal)(x.TransferFeePaid ?? 0d))
                        })
                        .OrderByDescending(g => g.Count)
                        .ToList();

                    ViewBag.ReportDescription = "Transfer count and fee amount by project.";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["Projects"] = grouped.Count.ToString("N0"),
                        ["Transfers"] = grouped.Sum(x => x.Count).ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Project", "Transfers", "Fee Received (PKR)" };
                    ViewBag.ReportRows = grouped.Select(g => new List<string>
                    {
                        g.Project,
                        g.Count.ToString("N0"),
                        g.FeePaid.ToString("N0")
                    }).ToList();
                    break;
                }
                case "transfer-amount-received":
                {
                    var transfers = await _context.Transfers
                        .AsNoTracking()
                        .Include(t => t.Customer)
                        .Where(t => (t.TransferFeePaid ?? 0d) > 0d)
                        .OrderByDescending(t => t.PaymentDate ?? t.CreatedAt)
                        .ToListAsync();

                    ViewBag.ReportDescription = "Transfer fee received entries.";
                    ViewBag.ReportSummary = new Dictionary<string, string>
                    {
                        ["Transfers with Receipt"] = transfers.Count.ToString("N0"),
                        ["Total Received"] = transfers.Sum(t => (decimal)(t.TransferFeePaid ?? 0d)).ToString("N0")
                    };
                    ViewBag.ReportColumns = new List<string> { "Transfer ID", "Customer", "Payment Date", "Payment Mode", "Challan No", "Fee Paid" };
                    ViewBag.ReportRows = transfers.Select(t => new List<string>
                    {
                        t.TransferID,
                        t.Customer?.FullName ?? t.CustomerID,
                        (t.PaymentDate ?? t.CreatedAt).ToString("MMM dd, yyyy"),
                        t.PaymentMode ?? "—",
                        t.PaymentChallanNo ?? "—",
                        ((decimal)(t.TransferFeePaid ?? 0d)).ToString("N0")
                    }).ToList();
                    break;
                }
                default:
                    ViewBag.ReportDescription = "Unknown report ID.";
                    break;
            }

            return View("RequestedReportLive");
        }
    }

    // View Models for Reports
    public class DefaulterReportViewModel
    {
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string PlanName { get; set; }
        public int? InstallmentNo { get; set; }
        public DateTime DueDate { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public int DaysOverdue { get; set; }
        public string Status { get; set; }
    }

    public class NotAllottedReportViewModel
    {
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CNIC { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string PlanName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public string PaymentStatus { get; set; }
        public string CustomerStatus { get; set; }
        public DateTime RegisteredDate { get; set; }
    }

    public class BlockedCustomerReportViewModel
    {
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CNIC { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string PlanName { get; set; }
        public DateTime RegisteredDate { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class PropertyReportViewModel
    {
        public string PropertyID { get; set; }
        public string ProjectName { get; set; }
        public string PlotNo { get; set; }
        public string Block { get; set; }
        public string Size { get; set; }
        public string PropertyType { get; set; }
        public string Status { get; set; }
        public bool IsAllotted { get; set; }
        public string AllottedTo { get; set; }
        public DateTime? AllottedDate { get; set; }
        public string Street { get; set; }
    }
}

