using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace PMS.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private const string ModuleKey = "Payment";
        private static readonly string[] AllowedPaymentAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private const long MaxPaymentAttachmentSize = 8 * 1024 * 1024; // 8MB
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private readonly ISurchargeService _surchargeService;
        private readonly IAmsPmsIntegrationService _amsPmsIntegration;

        public PaymentController(
            PMSDbContext context,
            IModulePermissionService modulePermission,
            ISurchargeService surchargeService,
            IAmsPmsIntegrationService amsPmsIntegration)
        {
            _context = context;
            _modulePermission = modulePermission;
            _surchargeService = surchargeService;
            _amsPmsIntegration = amsPmsIntegration;
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

        public async Task<IActionResult> Index()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var payments = await _context.Payments
                .Include(p => p.PaymentSchedule)
                    .ThenInclude(ps => ps.PaymentPlan)
                .ToListAsync();
            return View(payments);
        }

        // Payment Plans (Batches) Management
        public async Task<IActionResult> PaymentPlans()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var paymentPlans = await _context.PaymentPlans
                .Include(pp => pp.Project)
                .Include(pp => pp.Customers)
                .Include(pp => pp.PaymentSchedules)
                .ToListAsync();
            return View(paymentPlans);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentPlanDetails(string planId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(planId))
            {
                TempData["Error"] = "Plan ID is required.";
                return RedirectToAction(nameof(PaymentPlans));
            }

            var paymentPlan = await _context.PaymentPlans
                .Include(pp => pp.Project)
                .Include(pp => pp.Customers)
                .Include(pp => pp.PaymentSchedules)
                .FirstOrDefaultAsync(pp => pp.PlanID == planId);

            if (paymentPlan == null)
            {
                TempData["Error"] = "Payment plan not found.";
                return RedirectToAction(nameof(PaymentPlans));
            }

            return View(paymentPlan);
        }

        /// <summary>JSON for delete confirmation: customers, schedules, and whether payments block deletion.</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaymentPlanDeleteSummary(string planId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(planId))
            {
                return Json(new { found = false, message = "Plan ID is required." });
            }

            var plan = await _context.PaymentPlans
                .AsNoTracking()
                .Include(p => p.Project)
                .Include(p => p.Customers)
                .Include(p => p.PaymentSchedules)
                .FirstOrDefaultAsync(p => p.PlanID == planId);

            if (plan == null)
            {
                return Json(new { found = false, message = "Payment plan was not found." });
            }

            var scheduleIds = plan.PaymentSchedules?.Select(s => s.ScheduleID).Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
            var paymentCount = scheduleIds.Count == 0
                ? 0
                : await _context.Payments.AsNoTracking().CountAsync(p => p.ScheduleID != null && scheduleIds.Contains(p.ScheduleID));

            var customers = (plan.Customers ?? new List<Customer>())
                .OrderBy(c => c.FullName)
                .Take(100)
                .Select(c => new { customerId = c.CustomerID, fullName = c.FullName })
                .ToList();

            return Json(new
            {
                found = true,
                planId = plan.PlanID,
                planName = plan.PlanName,
                customerCount = plan.Customers?.Count ?? 0,
                customers,
                scheduleCount = plan.PaymentSchedules?.Count ?? 0,
                paymentCount,
                canDelete = paymentCount == 0,
                message = paymentCount > 0
                    ? $"This plan cannot be deleted because {paymentCount} payment record(s) are linked to its installments. Remove or reassign those payments first."
                    : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePaymentPlan(string planId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(planId))
            {
                TempData["Error"] = "Plan ID is required.";
                return RedirectToAction(nameof(PaymentPlans));
            }

            var plan = await _context.PaymentPlans
                .Include(p => p.Project)
                .Include(p => p.Customers)
                .Include(p => p.PaymentSchedules)
                .FirstOrDefaultAsync(p => p.PlanID == planId);

            if (plan == null)
            {
                TempData["Error"] = "Payment plan was not found.";
                return RedirectToAction(nameof(PaymentPlans));
            }

            var scheduleIds = plan.PaymentSchedules?.Select(s => s.ScheduleID).Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
            var paymentCount = scheduleIds.Count == 0
                ? 0
                : await _context.Payments.AsNoTracking().CountAsync(p => p.ScheduleID != null && scheduleIds.Contains(p.ScheduleID));

            if (paymentCount > 0)
            {
                TempData["Error"] =
                    $"Cannot delete plan \"{plan.PlanName}\": {paymentCount} payment record(s) are linked to its installments. Remove or reassign those payments first.";
                return RedirectToAction(nameof(PaymentPlans));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var customerList = (plan.Customers ?? new List<Customer>())
                .OrderBy(c => c.FullName)
                .Take(100)
                .Select(c => new { customerId = c.CustomerID, fullName = c.FullName })
                .ToList();
            var schedulesSnapshot = (plan.PaymentSchedules ?? new List<PaymentSchedule>())
                .OrderBy(s => s.InstallmentNo ?? int.MaxValue)
                .Select(s => new
                {
                    s.ScheduleID,
                    installmentNo = s.InstallmentNo,
                    s.PaymentDescription,
                    amountPkr = s.Amount,
                    amountUsd = s.AmountUSD,
                    dueDate = s.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    s.SurchargeApplied,
                    surchargeRate = s.SurchargeRate
                })
                .ToList();

            var auditPayload = new
            {
                eventType = "PaymentPlanDelete",
                timestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                actorUserId = userId,
                actorUserName = userName,
                planId = plan.PlanID,
                planName = plan.PlanName,
                projectId = plan.ProjectID,
                projectName = plan.Project?.ProjectName,
                totalAmountPkr = plan.TotalAmount,
                totalAmountUsd = plan.TotalAmountUSD,
                exchangeRate = plan.ExchangeRate,
                currency = plan.Currency,
                durationMonths = plan.DurationMonths,
                frequency = plan.Frequency,
                description = plan.Description,
                customerCount = plan.Customers?.Count ?? 0,
                customers = customerList,
                scheduleCount = plan.PaymentSchedules?.Count ?? 0,
                schedules = schedulesSnapshot,
                paymentsAttachedCount = paymentCount
            };
            var detailsJson = JsonSerializer.Serialize(auditPayload, jsonOptions);
            var actionSummary =
                $"Delete Payment Plan: {plan.PlanName} (PlanID {plan.PlanID}; {plan.Customers?.Count ?? 0} customer(s); {plan.PaymentSchedules?.Count ?? 0} schedule(s))";

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(plan.PlanID))
                {
                    await LogActivityAsync(userId, actionSummary, "PaymentPlan", plan.PlanID, detailsJson, saveImmediately: false);
                }

                _context.PaymentPlans.Remove(plan);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var custN = plan.Customers?.Count ?? 0;
                TempData["Success"] =
                    custN > 0
                        ? $"Payment plan \"{plan.PlanName}\" and all {plan.PaymentSchedules?.Count ?? 0} installment row(s) were deleted. {custN} customer(s) no longer have this plan assigned (Plan ID cleared)."
                        : $"Payment plan \"{plan.PlanName}\" and all {plan.PaymentSchedules?.Count ?? 0} installment row(s) were deleted.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Could not delete payment plan: {ex.Message}";
            }

            return RedirectToAction(nameof(PaymentPlans));
        }

        // Payment Schedules (Installments) Management
        public async Task<IActionResult> Schedules()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var schedules = await _context.PaymentSchedules
                .Include(ps => ps.PaymentPlan)
                    .ThenInclude(pp => pp.Customers)
                .Include(ps => ps.Payments)
                .OrderBy(ps => ps.DueDate)
                .ToListAsync();
            return View(schedules);
        }

        // Customer Payments - All payments received from customers  
        public async Task<IActionResult> CustomerPayments(string customerId = null, string auditFilter = "", string statusFilter = "")
        {
            return await CustomerPaymentsCore(customerId, auditFilter, statusFilter);
        }

        /// <summary>Payments whose payment Status is Pending.</summary>
        public async Task<IActionResult> PendingPayments(string customerId = null, string auditFilter = "")
        {
            return await CustomerPaymentsCore(customerId, auditFilter, statusFilter: "Pending");
        }

        private async Task<IActionResult> CustomerPaymentsCore(string customerId, string auditFilter, string statusFilter)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            // If customerId is provided, show specific customer
            if (!string.IsNullOrEmpty(customerId))
            {
                var customer = await _context.Customers
                    .Include(c => c.PaymentPlan)
                        .ThenInclude(pp => pp.PaymentSchedules)
                            .ThenInclude(ps => ps.Payments)
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId);

                if (customer == null)
                {
                    return NotFound();
                }

                ViewBag.SingleCustomer = true;
                return View("CustomerPaymentDetails", customer);
            }

            // Otherwise, show all customer payments (optionally filtered)
            var query = _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentSchedule)
                    .ThenInclude(ps => ps.PaymentPlan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(auditFilter))
            {
                if (string.Equals(auditFilter, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => p.AuditStatus == null || p.AuditStatus == "Pending");
                }
                else
                {
                    query = query.Where(p => p.AuditStatus == auditFilter);
                }
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(p => p.Status == statusFilter);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var isPendingPayments = string.Equals(statusFilter, "Pending", StringComparison.OrdinalIgnoreCase);
            ViewBag.AuditFilter = auditFilter;
            ViewBag.StatusFilter = statusFilter ?? "";
            ViewBag.IsPendingPayments = isPendingPayments;
            ViewBag.PendingAuditCount = await _context.Payments.CountAsync(p => p.AuditStatus == null || p.AuditStatus == "Pending");

            return View("CustomerPayments", payments);
        }

        /// <summary>Print-friendly payment receipt. Opens in new tab for printing.</summary>
        [HttpGet]
        public async Task<IActionResult> Receipt(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentSchedule)
                    .ThenInclude(ps => ps!.PaymentPlan)
                        .ThenInclude(pp => pp!.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        public async Task<IActionResult> PaymentSchedule(string planId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(planId))
            {
                return NotFound();
            }

            var paymentPlan = await _context.PaymentPlans
                .Include(pp => pp.PaymentSchedules)
                    .ThenInclude(ps => ps.Payments)
                .Include(pp => pp.Customers)
                .FirstOrDefaultAsync(pp => pp.PlanID == planId);

            if (paymentPlan == null)
            {
                return NotFound();
            }

            ViewBag.CustomersOnPlanCount = paymentPlan.Customers?.Count ?? 0;

            return View(paymentPlan);
        }

        /// <summary>
        /// AJAX: Returns customer info and due installments for the Record Payment form. Call after user enters Customer ID and clicks Search.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomerPaymentInfo(string customerId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(customerId))
            {
                return Json(new { found = false, message = "Please enter a Customer ID." });
            }

            try
            {
                var customerIdTrimmed = customerId.Trim();

                // char(10) keys may be space-padded — always compare with Trim()
                var customer = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.CustomerID != null
                        && c.CustomerID.Trim() == customerIdTrimmed
                        && (c.Status ?? "Active") == "Active")
                    .Select(c => new
                    {
                        CustomerID = c.CustomerID!,
                        c.FullName,
                        PlanID = c.PlanID
                    })
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return Json(new { found = false, message = "Customer not found or inactive." });
                }

                var planId = customer.PlanID?.Trim();
                if (string.IsNullOrWhiteSpace(planId))
                {
                    return Json(new { found = false, message = "Customer has no payment plan assigned." });
                }

                var planName = await _context.PaymentPlans
                    .AsNoTracking()
                    .Where(pp => pp.PlanID != null && pp.PlanID.Trim() == planId)
                    .Select(pp => pp.PlanName)
                    .FirstOrDefaultAsync();

                // Explicit plan filter + plan name from PaymentPlan (avoid wrong Include fix-up)
                var schedules = await (
                    from ps in _context.PaymentSchedules.AsNoTracking()
                    join pp in _context.PaymentPlans.AsNoTracking()
                        on ps.PlanID equals pp.PlanID
                    where ps.PlanID != null && ps.PlanID.Trim() == planId
                    orderby ps.DueDate, ps.InstallmentNo
                    select new
                    {
                        ps.ScheduleID,
                        PlanID = ps.PlanID!,
                        PlanName = pp.PlanName,
                        ps.PaymentDescription,
                        ps.InstallmentNo,
                        ps.DueDate,
                        ps.Amount,
                        ps.SurchargeApplied,
                        ps.SurchargeRate
                    }).ToListAsync();

                var scheduleIds = schedules.Select(ps => ps.ScheduleID).ToList();
                var paidBySchedule = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.ScheduleID != null
                        && scheduleIds.Contains(p.ScheduleID)
                        && p.CustomerID != null
                        && p.CustomerID.Trim() == customerIdTrimmed)
                    .GroupBy(p => p.ScheduleID)
                    .Select(g => new { ScheduleID = g.Key, TotalPaid = g.Sum(p => p.Amount) })
                    .ToListAsync();
                var paidLookup = paidBySchedule.ToDictionary(x => x.ScheduleID!, x => x.TotalPaid);

                var dueSchedules = schedules
                    .Select(ps =>
                    {
                        var paid = paidLookup.TryGetValue(ps.ScheduleID, out var p) ? p : 0m;
                        var outstanding = Math.Max(0m, ps.Amount - paid);
                        return new
                        {
                            scheduleId = ps.ScheduleID.Trim(),
                            planId = ps.PlanID.Trim(),
                            planName = string.IsNullOrWhiteSpace(ps.PlanName) ? planName : ps.PlanName,
                            paymentDescription = ps.PaymentDescription,
                            installmentNo = ps.InstallmentNo,
                            dueDate = ps.DueDate.ToString("dd-MM-yyyy"),
                            isOverdue = ps.DueDate.Date < DateTime.Today,
                            amount = ps.Amount,
                            paid = paid,
                            outstanding = outstanding,
                            surchargeApplied = ps.SurchargeApplied,
                            surchargeRate = ps.SurchargeRate
                        };
                    })
                    .Where(x => x.outstanding > 0)
                    .ToList();

                var totalDueSurcharge = await ComputeCustomerDueSurchargeAsync(
                    planId,
                    customerIdTrimmed,
                    DateTime.Now.Date);

                return Json(new
                {
                    found = true,
                    customerId = customerIdTrimmed,
                    fullName = string.IsNullOrWhiteSpace(customer.FullName) ? "(No Name)" : customer.FullName.Trim(),
                    planId = planId,
                    planName = planName ?? "",
                    schedules = dueSchedules,
                    totalDueSurcharge = totalDueSurcharge
                });
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Step 1: pick a customer (same filters as Customer list), then open RecordPayment for the selected row.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddPayment(string projectFilter = "All", string statusFilter = "All", string searchTerm = "")
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            projectFilter = string.IsNullOrWhiteSpace(projectFilter) ? "All" : projectFilter.Trim();
            statusFilter = string.IsNullOrWhiteSpace(statusFilter) ? "All" : statusFilter.Trim();
            searchTerm = searchTerm?.Trim() ?? "";

            var projects = await _context.Projects
                .AsNoTracking()
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.ProjectID, p.ProjectName })
                .ToListAsync();
            ViewBag.Projects = projects;
            ViewBag.ProjectFilter = projectFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchTerm = searchTerm;

            var query = _context.Customers
                .AsNoTracking()
                .Include(c => c.Project)
                .Include(c => c.PaymentPlan)
                    .ThenInclude(p => p!.Project)
                .Include(c => c.Allotments)
                .AsQueryable();

            if (!string.Equals(projectFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c =>
                    c.ProjectID == projectFilter ||
                    (c.PaymentPlan != null && c.PaymentPlan.ProjectID == projectFilter));
            }

            if (!string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(c =>
                    (c.CustomerID != null && c.CustomerID.ToLower().Contains(term)) ||
                    (c.FormNo != null && c.FormNo.ToLower().Contains(term)) ||
                    (c.FullName != null && c.FullName.ToLower().Contains(term)) ||
                    (c.CNIC != null && c.CNIC.ToLower().Contains(term)) ||
                    (c.Phone != null && c.Phone.ToLower().Contains(term)) ||
                    (c.MobileNo != null && c.MobileNo.ToLower().Contains(term)) ||
                    (c.MobileNo2 != null && c.MobileNo2.ToLower().Contains(term)) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .ThenBy(c => c.CustomerID)
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> RecordPayment(string scheduleId = null, string customerId = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            // Pre-fill from link (e.g. from Payment Schedule page): load schedule for info card and pass IDs for view to run search on load
            ViewBag.PreSelectedCustomerId = null;
            ViewBag.PreSelectedScheduleId = null;
            ViewBag.Schedule = null;
            ViewBag.ScheduleOutstanding = 0m;

            if (!string.IsNullOrWhiteSpace(scheduleId))
            {
                var preSelectedSchedule = await _context.PaymentSchedules
                    .AsNoTracking()
                    .Include(ps => ps.PaymentPlan)
                    .Include(ps => ps.Payments)
                    .FirstOrDefaultAsync(ps => ps.ScheduleID == scheduleId);

                if (preSelectedSchedule != null)
                {
                    ViewBag.Schedule = preSelectedSchedule;
                    ViewBag.PreSelectedScheduleId = scheduleId;
                    var paidAmount = preSelectedSchedule.Payments?.Sum(p => p.Amount) ?? 0m;
                    ViewBag.ScheduleOutstanding = Math.Max(0m, preSelectedSchedule.Amount - paidAmount);

                    if (!string.IsNullOrWhiteSpace(preSelectedSchedule.PlanID))
                    {
                        var preSelectedCustomerId = await _context.Customers
                            .AsNoTracking()
                            .Where(c => c.PlanID == preSelectedSchedule.PlanID && (c.Status ?? "Active") == "Active")
                            .OrderBy(c => c.CustomerID)
                            .Select(c => c.CustomerID)
                            .FirstOrDefaultAsync();
                        if (!string.IsNullOrWhiteSpace(preSelectedCustomerId))
                            ViewBag.PreSelectedCustomerId = preSelectedCustomerId;
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(customerId))
            {
                ViewBag.PreSelectedCustomerId = customerId.Trim();
            }

            ViewBag.PaymentStatuses = new List<string> { "Pending", "Paid", "Partially Paid", "Surcharge Paid" };
            ViewBag.BankNames = await GetBankNamesAsync();

            return View();
        }

        // Backward-compatible route alias for mistyped/deployed links.
        [HttpGet]
        public IActionResult RecordPaymentv(string scheduleId = null, string customerId = null)
        {
            return RedirectToAction(nameof(RecordPayment), new { scheduleId, customerId });
        }

        [HttpGet]
        public IActionResult RecordPayments(string scheduleId = null, string customerId = null)
        {
            return RedirectToAction(nameof(RecordPayment), new { scheduleId, customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(string customerId, string scheduleId, decimal amount, string method, string referenceNo, string bankName, DateTime? depositDate, string remarks, string status = "Paid", bool addExtraPayment = false, decimal extraAmount = 0m, IFormFile? proofAttachment = null, string? proofDescription = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(scheduleId))
            {
                TempData["Error"] = "Customer and installment are required. Search by Customer ID first and select an installment.";
                return RedirectToAction(nameof(RecordPayment));
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Amount must be greater than zero.";
                return RedirectToAction(nameof(RecordPayment), new { customerId, scheduleId });
            }

            if (string.IsNullOrWhiteSpace(referenceNo))
            {
                TempData["Error"] = "Reference Number is required.";
                return RedirectToAction(nameof(RecordPayment), new { customerId, scheduleId });
            }

            if (string.IsNullOrWhiteSpace(bankName))
            {
                TempData["Error"] = "Bank Name is required.";
                return RedirectToAction(nameof(RecordPayment), new { customerId, scheduleId });
            }

            if (addExtraPayment && extraAmount <= 0)
            {
                TempData["Error"] = "Extra amount must be greater than zero when Add Extra Payment is enabled.";
                return RedirectToAction(nameof(RecordPayment), new { customerId, scheduleId });
            }

            if (!addExtraPayment)
            {
                extraAmount = 0m;
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerID != null
                    && c.CustomerID.Trim() == customerId.Trim()
                    && (c.Status ?? "Active") == "Active")
                .Select(c => new { c.CustomerID, c.PlanID })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                TempData["Error"] = "Customer not found or inactive. Please search again.";
                return RedirectToAction(nameof(RecordPayment));
            }

            var customerPlanId = customer.PlanID?.Trim();
            var isSurchargePayment = string.Equals(scheduleId?.Trim(), "SURCHARGE_PAYMENT", StringComparison.OrdinalIgnoreCase);
            PaymentSchedule? schedule = null;
            decimal totalDue = 0m;
            string normalizedScheduleId;

            if (isSurchargePayment)
            {
                totalDue = await ComputeCustomerDueSurchargeAsync(
                    customerPlanId,
                    customerId.Trim(),
                    DateTime.Now.Date);

                if (totalDue <= 0m)
                {
                    TempData["Error"] = "No due surcharge found for this customer.";
                    return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim() });
                }

                if (amount > totalDue)
                {
                    TempData["Error"] = $"Amount Received (PKR {amount:N0}) must not exceed Total Due Surcharge (PKR {totalDue:N0}).";
                    return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim(), scheduleId = "SURCHARGE_PAYMENT" });
                }

                normalizedScheduleId = null;
            }
            else
            {
                var scheduleIdTrimmed = scheduleId?.Trim();
                schedule = await _context.PaymentSchedules
                    .AsNoTracking()
                    .Include(ps => ps.Payments)
                    .FirstOrDefaultAsync(ps => ps.ScheduleID != null && ps.ScheduleID.Trim() == scheduleIdTrimmed);

                if (schedule == null)
                {
                    TempData["Error"] = "Installment not found.";
                    return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim() });
                }

                if (!string.Equals(schedule.PlanID?.Trim(), customerPlanId, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Selected installment does not belong to this customer's plan.";
                    return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim() });
                }

                var paidSoFar = schedule.Payments?
                    .Where(p => p.CustomerID != null && p.CustomerID.Trim() == customerId.Trim())
                    .Sum(p => p.Amount) ?? 0m;
                totalDue = Math.Max(0m, schedule.Amount - paidSoFar);
                if (amount > totalDue)
                {
                    TempData["Error"] = $"Amount Received (PKR {amount:N0}) must not exceed Total Due for this installment (PKR {totalDue:N0}). You can record multiple partial payments.";
                    return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim(), scheduleId = scheduleIdTrimmed });
                }

                normalizedScheduleId = schedule.ScheduleID;
            }

            if (await HasCrossCustomerDuplicateReferenceAsync(customerId.Trim(), bankName, referenceNo))
            {
                TempData["Error"] = $"Reference Number '{referenceNo.Trim()}' is already used with bank '{bankName.Trim()}' for another customer.";
                return RedirectToAction(nameof(RecordPayment), new { customerId, scheduleId });
            }

            var payment = new Payment
            {
                PaymentID = GenerateID(),
                CustomerID = customerId.Trim(),
                ScheduleID = normalizedScheduleId,
                PaymentDate = DateTime.Now,
                DepositDate = (depositDate ?? DateTime.Today).Date,
                Amount = amount,
                Method = method,
                ReferenceNo = referenceNo.Trim(),
                BankName = bankName.Trim(),
                Status = isSurchargePayment ? "Surcharge Paid" : status,
                AccountHead = isSurchargePayment
                    ? "Surcharge Payment"
                    : (schedule?.PaymentDescription?.Trim()),
                Remarks = isSurchargePayment
                    ? (string.IsNullOrWhiteSpace(remarks) ? "Surcharge payment." : $"Surcharge payment. {remarks}")
                    : remarks
            };

            Payment? extraPayment = null;
            _context.Payments.Add(payment);
            if (extraAmount > 0m)
            {
                extraPayment = new Payment
                {
                    PaymentID = GenerateID(),
                    CustomerID = customerId.Trim(),
                    ScheduleID = null,
                    PaymentDate = DateTime.Now,
                    DepositDate = (depositDate ?? DateTime.Today).Date,
                    Amount = extraAmount,
                    Method = method,
                    ReferenceNo = referenceNo.Trim(),
                    BankName = bankName.Trim(),
                    Status = "Paid",
                    Remarks = string.IsNullOrWhiteSpace(remarks)
                        ? $"Extra payment received against {(isSurchargePayment ? "surcharge payment" : $"installment {scheduleId}")}."
                        : $"Extra payment received against {(isSurchargePayment ? "surcharge payment" : $"installment {scheduleId}")}. {remarks}"
                };
                _context.Payments.Add(extraPayment);
            }

            var amsUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _amsPmsIntegration.TryCreateArReceiptForCustomerPaymentAsync(payment, amsUserId);
            if (extraPayment != null)
                await _amsPmsIntegration.TryCreateArReceiptForCustomerPaymentAsync(extraPayment, amsUserId);

            await _context.SaveChangesAsync();

            if (proofAttachment != null && proofAttachment.Length > 0)
            {
                var uploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var attachmentError = await SavePaymentAttachmentAsync(payment.PaymentID, proofAttachment, proofDescription, uploadedBy);
                if (attachmentError != null)
                {
                    TempData["Error"] = attachmentError;
                    return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim(), scheduleId });
                }
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var actionText = string.IsNullOrEmpty(userName)
                    ? "Record Payment"
                    : $"Record Payment - {userName}";
                await LogActivityAsync(userId, actionText, "Payment", payment.PaymentID);

                if (extraPayment != null)
                {
                    var extraActionText = string.IsNullOrEmpty(userName)
                        ? "Record Extra Payment"
                        : $"Record Extra Payment - {userName}";
                    await LogActivityAsync(userId, extraActionText, "Payment", extraPayment.PaymentID);
                }
            }

            TempData["Success"] = extraAmount > 0m
                ? $"Payment of PKR {amount:N0} recorded and extra payment of PKR {extraAmount:N0} added. Outstanding for this {(isSurchargePayment ? "surcharge" : "installment")}: PKR {totalDue - amount:N0}."
                : $"Payment of PKR {amount:N0} recorded. Outstanding for this {(isSurchargePayment ? "surcharge" : "installment")}: PKR {totalDue - amount:N0}. You can record another payment if needed.";
            return RedirectToAction(nameof(RecordPayment), new { customerId = customerId.Trim(), scheduleId });
        }

        private async Task<decimal> ComputeCustomerDueSurchargeAsync(string? planId, string customerId, DateTime asOfDate)
        {
            if (string.IsNullOrWhiteSpace(planId) || string.IsNullOrWhiteSpace(customerId))
            {
                return 0m;
            }

            var planIdTrimmed = planId.Trim();
            var customerIdTrimmed = customerId.Trim();

            var schedules = await _context.PaymentSchedules
                .AsNoTracking()
                .Where(ps => ps.PlanID != null && ps.PlanID.Trim() == planIdTrimmed)
                .Select(ps => new PaymentSchedule
                {
                    ScheduleID = ps.ScheduleID,
                    Amount = ps.Amount,
                    DueDate = ps.DueDate,
                    SurchargeApplied = ps.SurchargeApplied,
                    SurchargeRate = ps.SurchargeRate
                })
                .ToListAsync();

            var scheduleIds = schedules
                .Select(s => s.ScheduleID)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.ScheduleID != null
                    && scheduleIds.Contains(p.ScheduleID)
                    && p.CustomerID != null
                    && p.CustomerID.Trim() == customerIdTrimmed)
                .ToListAsync();

            var paymentLookup = payments
                .GroupBy(p => p.ScheduleID ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var schedule in schedules)
            {
                if (!string.IsNullOrWhiteSpace(schedule.ScheduleID)
                    && paymentLookup.TryGetValue(schedule.ScheduleID, out var schedulePayments))
                {
                    schedule.Payments = schedulePayments;
                }
                else
                {
                    schedule.Payments = new List<Payment>();
                }
            }

            var surchargeBySchedule = _surchargeService.ComputeBySchedule(
                schedules,
                customerId.Trim(),
                asOfDate.Date);

            var totalCalculatedSurcharge = surchargeBySchedule.Values.Sum(x => x.Surcharge);

            var surchargeAdjustments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.CustomerID == customerId.Trim()
                    && p.ScheduleID == null
                    && p.AuditStatus == "Approved"
                    && (p.Status == null || p.Status != "Pending"))
                .ToListAsync();

            var paidSurchargeAmount = surchargeAdjustments
                .Where(p =>
                    p.Amount > 0m &&
                    (
                        string.Equals((p.AccountHead ?? string.Empty).Trim(), "Surcharge Payment", StringComparison.OrdinalIgnoreCase)
                        || (p.Remarks ?? string.Empty).Contains("Surcharge payment", StringComparison.OrdinalIgnoreCase)
                    ))
                .Sum(p => p.Amount);

            var waivedSurchargeAmount = surchargeAdjustments
                .Where(p =>
                    p.Amount < 0m &&
                    (
                        string.Equals((p.AccountHead ?? string.Empty).Trim(), "Surcharge Waived Off", StringComparison.OrdinalIgnoreCase)
                        || (
                            string.Equals((p.Method ?? string.Empty).Trim(), "Waiver", StringComparison.OrdinalIgnoreCase)
                            && (p.Remarks ?? string.Empty).Contains("AccountHead:Surcharge Waived Off", StringComparison.OrdinalIgnoreCase)
                        )
                    ))
                .Sum(p => Math.Abs(p.Amount));

            var netDueSurcharge = totalCalculatedSurcharge - paidSurchargeAmount - waivedSurchargeAmount;
            return Math.Max(0m, netDueSurcharge);
        }

        // ─── Multiple Payments ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> MultiplePayments(string? customerId = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            ViewBag.PreSelectedCustomerId = customerId;
            ViewBag.BankNames = await GetBankNamesAsync();
            return View();
        }

        public class MultiPaymentRow
        {
            public string? ScheduleId { get; set; }
            public decimal Amount { get; set; }
        }

        public class MultiPaymentRequest
        {
            public string? CustomerId { get; set; }
            public decimal TotalAmount { get; set; }
            public bool AddExtraPayment { get; set; }
            public decimal ExtraAmount { get; set; }
            public string? ReferenceNo { get; set; }
            public string? BankName { get; set; }
            public DateTime? DepositDate { get; set; }
            public string? Method { get; set; }
            public string? Remarks { get; set; }
            public DateTime PaymentDate { get; set; }
            public List<MultiPaymentRow>? Payments { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MultiplePayments([FromForm] string? payload, IFormFile? proofAttachment = null, string? proofDescription = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            try
            {
                var request = string.IsNullOrWhiteSpace(payload)
                    ? null
                    : JsonSerializer.Deserialize<MultiPaymentRequest>(payload, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (request == null || string.IsNullOrWhiteSpace(request.CustomerId))
                    return Json(new { success = false, message = "Customer ID is required." });

                if (string.IsNullOrWhiteSpace(request.ReferenceNo))
                    return Json(new { success = false, message = "Reference Number is required." });

                if (string.IsNullOrWhiteSpace(request.BankName))
                    return Json(new { success = false, message = "Bank Name is required." });

                var rows = request.Payments?.Where(p => p.Amount > 0).ToList() ?? new List<MultiPaymentRow>();
                var extraAmount = request.AddExtraPayment ? request.ExtraAmount : 0m;
                if (request.AddExtraPayment && extraAmount <= 0)
                    return Json(new { success = false, message = "Extra amount must be greater than zero when Add Extra Payment is enabled." });
                if (!rows.Any() && extraAmount <= 0)
                    return Json(new { success = false, message = "Enter at least one installment amount or add an extra payment." });

                // Validate sum equals total
                var sumRows = rows.Sum(p => p.Amount);
                var distributedTotal = sumRows + extraAmount;
                if (Math.Abs(distributedTotal - request.TotalAmount) > 0.01m)
                    return Json(new { success = false, message = $"Installment amounts (PKR {sumRows:N0}) plus extra payment (PKR {extraAmount:N0}) must equal Total Payment (PKR {request.TotalAmount:N0})." });

                var customer = await _context.Customers.AsNoTracking()
                    .Where(c => c.CustomerID == request.CustomerId.Trim() && (c.Status ?? "Active") == "Active")
                    .Select(c => new { c.CustomerID, c.PlanID })
                    .FirstOrDefaultAsync();

                if (customer == null)
                    return Json(new { success = false, message = "Customer not found or inactive." });

                if (await HasCrossCustomerDuplicateReferenceAsync(request.CustomerId.Trim(), request.BankName, request.ReferenceNo))
                    return Json(new { success = false, message = $"Reference Number '{request.ReferenceNo?.Trim()}' is already used with bank '{request.BankName?.Trim()}' for another customer." });

                var scheduleIds = rows.Select(r => r.ScheduleId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

                var schedules = await _context.PaymentSchedules.AsNoTracking()
                    .Include(ps => ps.Payments)
                    .Where(ps => scheduleIds.Contains(ps.ScheduleID) && ps.PlanID == customer.PlanID)
                    .ToListAsync();

                if (schedules.Count != scheduleIds.Count)
                    return Json(new { success = false, message = "One or more selected installments do not belong to this customer's plan." });

                var paymentDate = request.PaymentDate == default ? DateTime.Now : request.PaymentDate;
                var depositDate = (request.DepositDate ?? DateTime.Today).Date;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
                var savedIds = new List<string>();
                var stagedForAms = new List<Payment>();

                foreach (var row in rows)
                {
                    var schedule = schedules.First(s => s.ScheduleID == row.ScheduleId);
                    var paidSoFar = schedule.Payments?.Where(p => p.CustomerID == request.CustomerId.Trim()).Sum(p => p.Amount) ?? 0m;
                    var outstanding = Math.Max(0m, schedule.Amount - paidSoFar);

                    if (row.Amount > outstanding + 0.01m)
                        return Json(new { success = false, message = $"Amount for Installment #{schedule.InstallmentNo} (PKR {row.Amount:N0}) exceeds outstanding (PKR {outstanding:N0})." });

                    var isFullPayment = row.Amount >= outstanding - 0.01m;
                    var status = isFullPayment ? "Paid" : "Partially Paid";

                    var payment = new Payment
                    {
                        PaymentID   = GenerateID(),
                        CustomerID  = request.CustomerId.Trim(),
                        ScheduleID  = row.ScheduleId,
                        PaymentDate = paymentDate,
                        DepositDate = depositDate,
                        Amount      = row.Amount,
                        Method      = request.Method,
                        ReferenceNo = request.ReferenceNo?.Trim(),
                        BankName    = request.BankName?.Trim(),
                        Status      = status,
                        Remarks     = request.Remarks
                    };

                    _context.Payments.Add(payment);
                    savedIds.Add(payment.PaymentID);
                    stagedForAms.Add(payment);
                }

                if (extraAmount > 0m)
                {
                    var extraPayment = new Payment
                    {
                        PaymentID = GenerateID(),
                        CustomerID = request.CustomerId.Trim(),
                        ScheduleID = null,
                        PaymentDate = paymentDate,
                        DepositDate = depositDate,
                        Amount = extraAmount,
                        Method = request.Method,
                        ReferenceNo = request.ReferenceNo?.Trim(),
                        BankName = request.BankName?.Trim(),
                        Status = "Paid",
                        Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                            ? "Extra payment received."
                            : $"Extra payment received. {request.Remarks}"
                    };
                    _context.Payments.Add(extraPayment);
                    savedIds.Add(extraPayment.PaymentID);
                    stagedForAms.Add(extraPayment);
                }

                foreach (var p in stagedForAms)
                    await _amsPmsIntegration.TryCreateArReceiptForCustomerPaymentAsync(p, userId);

                await _context.SaveChangesAsync();

                if (proofAttachment != null && proofAttachment.Length > 0)
                {
                    var uploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    foreach (var paymentId in savedIds)
                    {
                        var attachmentError = await SavePaymentAttachmentAsync(paymentId, proofAttachment, proofDescription, uploadedBy);
                        if (attachmentError != null)
                        {
                            return Json(new { success = false, message = attachmentError });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    var actionText = string.IsNullOrEmpty(userName)
                        ? $"Record Multiple Payments ({rows.Count} installments)"
                        : $"Record Multiple Payments ({rows.Count} installments) - {userName}";
                    foreach (var pid in savedIds)
                        await LogActivityAsync(userId, actionText, "Payment", pid);
                }

                return Json(new { success = true, count = savedIds.Count, message = $"{savedIds.Count} payment(s) recorded successfully (PKR {distributedTotal:N0} total)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment(string paymentId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                TempData["Error"] = "Payment ID is required.";
                return RedirectToAction(nameof(CustomerPayments));
            }

            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction(nameof(CustomerPayments));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var actionText = $"Delete Payment. CustomerID: {payment.CustomerID ?? ""}";
                await LogActivityAsync(userId, actionText, "Payment", payment.PaymentID);
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment deleted successfully.";
            return RedirectToAction(nameof(CustomerPayments));
        }

        [HttpGet]
        public async Task<IActionResult> EditPayment(string paymentId)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                TempData["Error"] = "Payment ID is required.";
                return RedirectToAction(nameof(CustomerPayments));
            }

            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentSchedule)
                    .ThenInclude(ps => ps.PaymentPlan)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction(nameof(CustomerPayments));
            }

            if (payment.PaymentSchedule != null)
            {
                var otherPaymentsSum = await _context.Payments
                    .Where(p => p.ScheduleID == payment.ScheduleID && p.PaymentID != paymentId)
                    .SumAsync(p => p.Amount);
                ViewBag.MaxAmount = payment.PaymentSchedule.Amount - otherPaymentsSum;
            }
            else
            {
                ViewBag.MaxAmount = payment.Amount;
            }

            ViewBag.PaymentStatuses = new List<string> { "Pending", "Paid", "Partially Paid" };
            ViewBag.BankNames = await GetBankNamesAsync();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayment(string paymentId, DateTime? depositDate, decimal amount, string method, string referenceNo, string bankName, string status, string remarks, IFormFile? proofAttachment = null, string? proofDescription = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                TempData["Error"] = "Payment ID is required.";
                return RedirectToAction(nameof(CustomerPayments));
            }

            var payment = await _context.Payments
                .Include(p => p.PaymentSchedule)
                    .ThenInclude(ps => ps.PaymentPlan)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction(nameof(CustomerPayments));
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Amount must be greater than zero.";
                return RedirectToAction(nameof(EditPayment), new { paymentId });
            }

            if (string.IsNullOrWhiteSpace(referenceNo))
            {
                TempData["Error"] = "Reference Number is required.";
                return RedirectToAction(nameof(EditPayment), new { paymentId });
            }

            if (string.IsNullOrWhiteSpace(bankName))
            {
                TempData["Error"] = "Bank Name is required.";
                return RedirectToAction(nameof(EditPayment), new { paymentId });
            }

            var schedule = payment.PaymentSchedule;
            if (schedule != null)
            {
                var otherPaymentsSum = await _context.Payments
                    .Where(p => p.ScheduleID == schedule.ScheduleID && p.PaymentID != paymentId)
                    .SumAsync(p => p.Amount);
                var maxAllowed = schedule.Amount - otherPaymentsSum;
                if (amount > maxAllowed)
                {
                    TempData["Error"] = $"Amount (PKR {amount:N0}) must not exceed the remaining due for this installment (PKR {maxAllowed:N0}).";
                    return RedirectToAction(nameof(EditPayment), new { paymentId });
                }
            }

            if (await HasCrossCustomerDuplicateReferenceAsync(payment.CustomerID?.Trim(), bankName, referenceNo, paymentId))
            {
                TempData["Error"] = $"Reference Number '{referenceNo.Trim()}' is already used with bank '{bankName.Trim()}' for another customer.";
                return RedirectToAction(nameof(EditPayment), new { paymentId });
            }

            payment.DepositDate = (depositDate ?? payment.DepositDate ?? DateTime.Today).Date;
            payment.Amount = amount;
            payment.Method = method;
            payment.ReferenceNo = referenceNo.Trim();
            payment.BankName = bankName.Trim();
            payment.Status = status;
            payment.Remarks = remarks;

            await _context.SaveChangesAsync();

            if (proofAttachment != null && proofAttachment.Length > 0)
            {
                var uploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var attachmentError = await SavePaymentAttachmentAsync(payment.PaymentID, proofAttachment, proofDescription, uploadedBy);
                if (attachmentError != null)
                {
                    TempData["Error"] = attachmentError;
                    return RedirectToAction(nameof(EditPayment), new { paymentId });
                }
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
                var actionText = string.IsNullOrEmpty(userName) ? "Edit Payment" : $"Edit Payment - {userName}";
                await LogActivityAsync(userId, actionText, "Payment", payment.PaymentID);
            }

            TempData["Success"] = "Payment updated successfully.";
            return RedirectToAction(nameof(CustomerPayments));
        }

        [HttpGet]
        public async Task<IActionResult> CreatePaymentPlan()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            ViewBag.Projects = await _context.Projects.ToListAsync();
            ViewBag.UsdToPkrRate = await GetUsdToPkrRateAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentPlan([FromBody] PaymentPlanCreateViewModel viewModel)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    || string.Equals(Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
                    || (Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return Json(new { success = false, message = "You do not have permission to create a payment plan." });
                }
                return denied;
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    var modelErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct()
                        .ToList();
                    var modelMessage = modelErrors.Count > 0
                        ? string.Join(" ", modelErrors)
                        : "Validation failed. Please check the form and try again.";
                    return Json(new { success = false, message = modelMessage });
                }

                const decimal MaxAmountPkr = 1_000_000_000m; // 100 crores
                static bool IsWholeAmount(decimal amount) => decimal.Truncate(amount) == amount;

                if (viewModel?.PaymentPlan == null || viewModel?.PaymentSchedules == null)
                {
                    return Json(new { success = false, message = "Invalid data provided. Payment plan or schedule data is missing." });
                }

                var planData = viewModel.PaymentPlan;
                if (string.IsNullOrWhiteSpace(planData.ProjectID))
                {
                    return Json(new { success = false, message = "Project is required." });
                }
                if (string.IsNullOrWhiteSpace(planData.SubProject))
                {
                    return Json(new { success = false, message = "Subproject is required." });
                }
                var scheduleData = viewModel.PaymentSchedules;

                var projectMeta = await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.ProjectID == planData.ProjectID)
                    .Select(p => new { p.SubProjects })
                    .FirstOrDefaultAsync();
                if (projectMeta == null)
                {
                    return Json(new { success = false, message = "Selected project was not found." });
                }

                var allowedSubProjects = SplitCsvValues(projectMeta.SubProjects);
                if (allowedSubProjects.Count > 0 && !allowedSubProjects.Contains(planData.SubProject.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Selected subproject is not available for this project." });
                }

                var exchangeRate = planData.ExchangeRate > 0 ? planData.ExchangeRate : await GetUsdToPkrRateAsync();
                var hasExchangeRate = exchangeRate > 0;

                if (planData.TotalAmount <= 0)
                {
                    return Json(new { success = false, message = "Total amount (PKR) must be greater than zero." });
                }
                if (!IsWholeAmount(planData.TotalAmount))
                {
                    return Json(new { success = false, message = "Total amount (PKR) must be a whole number (no decimals)." });
                }
                if (planData.TotalAmount > MaxAmountPkr)
                {
                    return Json(new { success = false, message = "Total amount (PKR) cannot exceed 100 crores (1,000,000,000)." });
                }

                // Validate total amount: Token + Installments must not exceed Plan Total Amount
                var totalInstallments = scheduleData.TotalInstallments;
                var totalPlanAmount = planData.TotalAmount;
                var tokenAmount = scheduleData.IncludeToken && scheduleData.TokenAmount.HasValue ? scheduleData.TokenAmount.Value : 0;
                if (scheduleData.IncludeToken)
                {
                    if (!scheduleData.TokenAmount.HasValue || scheduleData.TokenAmount.Value <= 0)
                    {
                        return Json(new { success = false, message = "Token amount (PKR) must be greater than zero when token is included." });
                    }
                    if (!IsWholeAmount(tokenAmount))
                    {
                        return Json(new { success = false, message = "Token amount (PKR) must be a whole number (no decimals)." });
                    }
                    if (tokenAmount > MaxAmountPkr)
                    {
                        return Json(new { success = false, message = "Token amount (PKR) cannot exceed 100 crores (1,000,000,000)." });
                    }
                }
                decimal? tokenAmountUSD = null;
                if (scheduleData.IncludeToken)
                {
                    tokenAmountUSD = hasExchangeRate
                        ? Math.Round(tokenAmount / exchangeRate, 2, MidpointRounding.AwayFromZero)
                        : null;
                }
                var possessionAmount = scheduleData.PossessionAmount.GetValueOrDefault();
                if (possessionAmount < 0)
                {
                    return Json(new { success = false, message = "Possession amount cannot be negative." });
                }
                if (!IsWholeAmount(possessionAmount))
                {
                    return Json(new { success = false, message = "Possession amount (PKR) must be a whole number (no decimals)." });
                }
                if (possessionAmount > MaxAmountPkr)
                {
                    return Json(new { success = false, message = "Possession amount (PKR) cannot exceed 100 crores (1,000,000,000)." });
                }
                if (possessionAmount > 0 && string.IsNullOrWhiteSpace(scheduleData.PossessionPaymentDescription))
                {
                    return Json(new { success = false, message = "Possession account head is required when possession amount is greater than zero." });
                }
                decimal? possessionAmountUSD = null;
                if (possessionAmount > 0)
                {
                    possessionAmountUSD = hasExchangeRate
                        ? Math.Round(possessionAmount / exchangeRate, 2, MidpointRounding.AwayFromZero)
                        : null;
                }

                if (totalInstallments <= 0)
                {
                    return Json(new { success = false, message = "Total installments must be greater than zero." });
                }

                var distributableAmount = totalPlanAmount - tokenAmount;
                if (distributableAmount < 0)
                {
                    return Json(new { success = false, message = "Token amount cannot exceed total plan amount." });
                }

                var installmentAmount = scheduleData.InstallmentAmount;
                installmentAmount = Math.Round(installmentAmount, 2, MidpointRounding.AwayFromZero);
                if (installmentAmount <= 0)
                {
                    return Json(new { success = false, message = "Installment amount (PKR) must be greater than zero." });
                }
                if (!IsWholeAmount(installmentAmount))
                {
                    return Json(new { success = false, message = "Installment amount (PKR) must be a whole number (no decimals)." });
                }
                if (installmentAmount > MaxAmountPkr)
                {
                    return Json(new { success = false, message = "Installment amount (PKR) cannot exceed 100 crores (1,000,000,000)." });
                }

                decimal? installmentAmountUSD = hasExchangeRate
                    ? Math.Round(installmentAmount / exchangeRate, 2, MidpointRounding.AwayFromZero)
                    : null;

                var totalInstallmentsAmount = Math.Round(totalInstallments * installmentAmount, 2, MidpointRounding.AwayFromZero);
                decimal grandTotal = totalInstallmentsAmount + tokenAmount + possessionAmount;

                if (grandTotal > planData.TotalAmount)
                {
                    decimal excess = grandTotal - planData.TotalAmount;
                    return Json(new { 
                        success = false, 
                        message = $"Total Installments Amount (PKR {totalInstallmentsAmount:N0}) + Token Amount (PKR {tokenAmount:N0}) + Possession Amount (PKR {possessionAmount:N0}) = PKR {grandTotal:N0} exceeds Parent Total Amount (PKR {planData.TotalAmount:N0}). Excess: PKR {excess:N0}." 
                    });
                }

                // Create PaymentPlan
                var paymentPlan = new PaymentPlan
                {
                    PlanID = GenerateID(),
                    PlanName = planData.PlanName,
                    ProjectID = string.IsNullOrEmpty(planData.ProjectID) ? null : planData.ProjectID,
                    // Size remains on the CreatePaymentPlan UI but is not used when saving the plan.
                    RegisteredSize = null,
                    SubProject = planData.SubProject?.Trim(),
                    TotalAmount = planData.TotalAmount,
                    TotalAmountUSD = hasExchangeRate
                        ? Math.Round(planData.TotalAmount / exchangeRate, 2, MidpointRounding.AwayFromZero)
                        : null,
                    ExchangeRate = exchangeRate,
                    Currency = planData.Currency,
                    DurationMonths = planData.DurationMonths,
                    Frequency = planData.Frequency,
                    Description = planData.Description,
                    CreatedAt = DateTime.Now
                };

                _context.PaymentPlans.Add(paymentPlan);
                await _context.SaveChangesAsync();

                // Create PaymentSchedules
                var schedules = new List<PaymentSchedule>();
                var includeToken = scheduleData.IncludeToken;
                var frequency = scheduleData.Frequency ?? "Monthly";
                var firstDueDate = scheduleData.FirstInstallmentDueDate;
                var paymentDescription = scheduleData.PaymentDescription ?? "Installment";
                var tokenPaymentDescription = string.IsNullOrWhiteSpace(scheduleData.TokenPaymentDescription)
                    ? "Token"
                    : scheduleData.TokenPaymentDescription.Trim();
                var possessionPaymentDescription = string.IsNullOrWhiteSpace(scheduleData.PossessionPaymentDescription)
                    ? "Possession"
                    : scheduleData.PossessionPaymentDescription.Trim();
                var surchargeApplied = scheduleData.SurchargeApplied;
                var surchargeRate = scheduleData.SurchargeRate;
                if (surchargeApplied && surchargeRate <= 0m)
                {
                    surchargeRate = 0.05m;
                }
                // Surcharge input applies to regular installments only.
                // Token and Possession are always created with surcharge 0.
                var regularInstallmentSurchargeApplied = surchargeApplied;
                var regularInstallmentSurchargeRate = regularInstallmentSurchargeApplied ? surchargeRate : 0m;

                int installmentNo = 0;

                // Create Token if included (Installment 0 - no surcharge)
                if (includeToken && scheduleData.TokenAmount.HasValue)
                {
                    if (!scheduleData.TokenDueDate.HasValue || scheduleData.TokenDueDate.Value == default)
                    {
                        return Json(new { success = false, message = "Token due date is required when token payment is included." });
                    }

                    var tokenSchedule = new PaymentSchedule
                    {
                        ScheduleID = GenerateID(),
                        PlanID = paymentPlan.PlanID,
                        PaymentDescription = tokenPaymentDescription,
                        InstallmentNo = 0,
                        DueDate = scheduleData.TokenDueDate.Value.Date,
                        Amount = scheduleData.TokenAmount.Value,
                        AmountUSD = tokenAmountUSD,
                        SurchargeApplied = false, // Token has no surcharge
                        SurchargeRate = 0m // Token has no surcharge rate
                    };
                    schedules.Add(tokenSchedule);
                    installmentNo = 1;
                }

                // Create regular installments
                for (int i = 0; i < totalInstallments; i++)
                {
                    var dueDate = CalculateDueDate(firstDueDate, frequency, i);
                    
                    var schedule = new PaymentSchedule
                    {
                        ScheduleID = GenerateID(),
                        PlanID = paymentPlan.PlanID,
                        PaymentDescription = paymentDescription,
                        InstallmentNo = installmentNo,
                        DueDate = dueDate,
                        Amount = installmentAmount,
                        AmountUSD = installmentAmountUSD,
                        SurchargeApplied = regularInstallmentSurchargeApplied,
                        SurchargeRate = regularInstallmentSurchargeRate
                    };
                    schedules.Add(schedule);
                    installmentNo++;
                }

                // Create Possession as the last payment (manual, no surcharge)
                if (possessionAmount > 0)
                {
                    if (!scheduleData.PossessionDueDate.HasValue || scheduleData.PossessionDueDate.Value == default)
                    {
                        return Json(new { success = false, message = "Possession due date is required when possession amount is greater than zero." });
                    }

                    var possessionSchedule = new PaymentSchedule
                    {
                        ScheduleID = GenerateID(),
                        PlanID = paymentPlan.PlanID,
                        PaymentDescription = possessionPaymentDescription,
                        InstallmentNo = installmentNo,
                        DueDate = scheduleData.PossessionDueDate.Value.Date,
                        Amount = Math.Round(possessionAmount, 2, MidpointRounding.AwayFromZero),
                        AmountUSD = possessionAmountUSD,
                        SurchargeApplied = false,
                        SurchargeRate = 0m
                    };
                    schedules.Add(possessionSchedule);
                }

                _context.PaymentSchedules.AddRange(schedules);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivityAsync(userId, "Create Payment Plan", "PaymentPlan", paymentPlan.PlanID);
                }

                return Json(new
                {
                    success = true,
                    planId = paymentPlan.PlanID,
                    planName = paymentPlan.PlanName,
                    scheduleCount = schedules.Count,
                    message = $"Payment plan \"{paymentPlan.PlanName}\" saved successfully with {schedules.Count} schedule(s)."
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Could not save payment plan: " + detail });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectSizes(string projectId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Json(Array.Empty<string>());
            }

            var projectSizes = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.Sizes)
                .FirstOrDefaultAsync();

            return Json(SplitCsvValues(projectSizes));
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectSubProjects(string projectId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Json(Array.Empty<string>());
            }

            var mapped = await _context.ProjectSubProjects
                .AsNoTracking()
                .Where(s => s.ProjectID == projectId)
                .OrderBy(s => s.SubProjectName)
                .Select(s => s.SubProjectName)
                .ToArrayAsync();
            if (mapped.Length > 0)
                return Json(mapped);

            var projectSubProjects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.SubProjects)
                .FirstOrDefaultAsync();

            return Json(SplitCsvValues(projectSubProjects));
        }

        private DateTime CalculateDueDate(DateTime firstDueDate, string frequency, int installmentIndex)
        {
            return frequency switch
            {
                "Monthly" => firstDueDate.AddMonths(installmentIndex),
                "Quarterly" => firstDueDate.AddMonths(installmentIndex * 3),
                "Half Yearly" => firstDueDate.AddMonths(installmentIndex * 6),
                "Yearly" => firstDueDate.AddYears(installmentIndex),
                _ => firstDueDate.AddMonths(installmentIndex)
            };
        }

        private async Task<decimal> GetUsdToPkrRateAsync()
        {
            const decimal defaultRate = 1m;
            var config = await _context.Configurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigKey == "Currency:USDToPKR");

            if (config != null && decimal.TryParse(config.ConfigValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            return defaultRate;
        }

        private static List<string> SplitCsvValues(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return new List<string>();
            }

            return csv
                .Split(',')
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<string>> GetBankNamesAsync()
        {
            var configValue = await _context.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey == "banks")
                .Select(c => c.ConfigValue)
                .FirstOrDefaultAsync();

            return SplitCsvValues(configValue);
        }

        private async Task<List<string>> GetPaymentHeadOptionsAsync(string? currentValue = null)
        {
            var configValue = await _context.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey == "PaymentHeads")
                .Select(c => c.ConfigValue)
                .FirstOrDefaultAsync();

            var heads = SplitCsvValues(configValue);
            var current = currentValue?.Trim();
            if (!string.IsNullOrWhiteSpace(current)
                && !heads.Any(h => string.Equals(h, current, StringComparison.OrdinalIgnoreCase)))
            {
                heads.Insert(0, current);
            }

            return heads;
        }

        private async Task<bool> HasCrossCustomerDuplicateReferenceAsync(string? customerId, string? bankName, string? referenceNo, string? excludePaymentId = null)
        {
            var normalizedCustomerId = (customerId ?? string.Empty).Trim();
            var normalizedBankName = (bankName ?? string.Empty).Trim();
            var normalizedReferenceNo = (referenceNo ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedCustomerId)
                || string.IsNullOrWhiteSpace(normalizedBankName)
                || string.IsNullOrWhiteSpace(normalizedReferenceNo))
            {
                return false;
            }

            var query = _context.Payments
                .AsNoTracking()
                .Where(p => p.BankName != null
                    && p.ReferenceNo != null
                    && p.CustomerID != null
                    && p.BankName.Trim() == normalizedBankName
                    && p.ReferenceNo.Trim() == normalizedReferenceNo
                    && p.CustomerID.Trim() != normalizedCustomerId);

            if (!string.IsNullOrWhiteSpace(excludePaymentId))
            {
                var paymentId = excludePaymentId.Trim();
                query = query.Where(p => p.PaymentID != paymentId);
            }

            return await query.AnyAsync();
        }

        [HttpGet]
        public async Task<IActionResult> CreatePaymentSchedule(string planId)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var paymentPlan = await _context.PaymentPlans.FindAsync(planId);
            if (paymentPlan == null)
            {
                return NotFound();
            }

            ViewBag.PaymentPlan = paymentPlan;
            return View(new PaymentSchedule
            {
                PlanID = planId,
                SurchargeApplied = true,
                SurchargeRate = 0.05m
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentSchedule(PaymentSchedule schedule)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            // Surcharge rate is handled as a fractional value end-to-end (e.g. 0.05 = 5%).
            if (schedule.SurchargeApplied && schedule.SurchargeRate < 0m)
            {
                schedule.SurchargeRate = 0.05m;
            }

            // Server-side guard: total of installments must not exceed plan total
            var plan = await _context.PaymentPlans
                .Include(p => p.PaymentSchedules)
                .FirstOrDefaultAsync(p => p.PlanID == schedule.PlanID);

            if (plan == null)
            {
                return NotFound();
            }

            var existingTotal = plan.PaymentSchedules.Sum(ps => ps.Amount);
            var projectedTotal = existingTotal + schedule.Amount;
            if (projectedTotal > plan.TotalAmount)
            {
                var remaining = plan.TotalAmount - existingTotal;
                ModelState.AddModelError("Amount", $"Installments total would exceed plan total. Remaining allowed: {remaining:N0} PKR.");
            }

            var exchangeRate = plan.ExchangeRate.GetValueOrDefault();
            schedule.AmountUSD = exchangeRate > 0
                ? Math.Round(schedule.Amount / exchangeRate, 2, MidpointRounding.AwayFromZero)
                : null;

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentPlan = plan;
                return View(schedule);
            }

            schedule.ScheduleID = GenerateID();
            _context.PaymentSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivityAsync(userId, "Create Payment Schedule", "PaymentSchedule", schedule.ScheduleID);
            }

            TempData["Success"] = "Installment created successfully.";
            return RedirectToAction(nameof(PaymentSchedule), new { planId = schedule.PlanID });
        }

        [HttpGet]
        public async Task<IActionResult> EditPaymentSchedule(string scheduleId)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(scheduleId))
                return NotFound();

            var schedule = await _context.PaymentSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.ScheduleID == scheduleId);

            if (schedule == null)
                return NotFound();

            var plan = await _context.PaymentPlans
                .AsNoTracking()
                .Include(p => p.PaymentSchedules)
                .FirstOrDefaultAsync(p => p.PlanID == schedule.PlanID);

            if (plan == null)
                return NotFound();

            var customersOnPlanCount = await _context.Customers
                .AsNoTracking()
                .CountAsync(c => c.PlanID == schedule.PlanID);

            ViewBag.PaymentPlan = plan;
            ViewBag.CustomersOnPlanCount = customersOnPlanCount;
            ViewBag.OthersAmountSum = plan.PaymentSchedules
                .Where(ps => ps.ScheduleID != schedule.ScheduleID)
                .Sum(ps => ps.Amount);
            ViewBag.PaymentHeads = await GetPaymentHeadOptionsAsync(schedule.PaymentDescription);

            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPaymentSchedule(
            PaymentSchedule schedule,
            bool adjustPlanTotal = false,
            decimal? newPlanTotalPkr = null,
            string? changeReason = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (schedule.SurchargeApplied && schedule.SurchargeRate < 0m)
            {
                schedule.SurchargeRate = 0.05m;
            }

            if (schedule.SurchargeApplied && (schedule.SurchargeRate < 0m || schedule.SurchargeRate > 100m))
            {
                TempData["Error"] = "Surcharge rate must be between 0 and 100.";
                return RedirectToAction(nameof(EditPaymentSchedule), new { scheduleId = schedule.ScheduleID });
            }

            var plan = await _context.PaymentPlans
                .Include(p => p.PaymentSchedules)
                .FirstOrDefaultAsync(p => p.PlanID == schedule.PlanID);
            if (plan == null)
                return NotFound();

            var existingSchedule = plan.PaymentSchedules.FirstOrDefault(ps => ps.ScheduleID == schedule.ScheduleID);
            if (existingSchedule == null)
                return NotFound();

            var exchangeRate = plan.ExchangeRate.GetValueOrDefault();
            schedule.AmountUSD = exchangeRate > 0
                ? Math.Round(schedule.Amount / exchangeRate, 2, MidpointRounding.AwayFromZero)
                : null;

            var customersAssignedCount = await _context.Customers.AsNoTracking()
                .CountAsync(c => c.PlanID == schedule.PlanID);

            var totalWithoutThis = plan.PaymentSchedules.Where(ps => ps.ScheduleID != schedule.ScheduleID).Sum(ps => ps.Amount);
            var projectedSchedulesTotal = Math.Round(totalWithoutThis + schedule.Amount, 2, MidpointRounding.AwayFromZero);
            var planTotalExceeded = projectedSchedulesTotal > Math.Round(plan.TotalAmount, 2, MidpointRounding.AwayFromZero);
            var isAmountIncrease = schedule.Amount > existingSchedule.Amount + 0.000001m;
            var requiresPlanTotalAdjustment = planTotalExceeded && isAmountIncrease;

            if (requiresPlanTotalAdjustment && !adjustPlanTotal)
            {
                var remaining = plan.TotalAmount - totalWithoutThis;
                TempData["Error"] =
                    $"Installments total would exceed the plan total (remaining for this installment: PKR {remaining:N0}). " +
                    $"{customersAssignedCount} customer(s) are assigned to this plan. " +
                    "Open the installment again, enable \"Increase payment plan total\", enter a reason, and save — or reduce the amount.";
                return RedirectToAction(nameof(PaymentSchedule), new { planId = schedule.PlanID });
            }

            decimal? newPlanTotalApplied = null;
            if (requiresPlanTotalAdjustment && adjustPlanTotal)
            {
                if (string.IsNullOrWhiteSpace(changeReason))
                {
                    TempData["Error"] = "When increasing the payment plan total, a written reason is required (audit trail).";
                    return RedirectToAction(nameof(PaymentSchedule), new { planId = schedule.PlanID });
                }

                var targetTotal = newPlanTotalPkr.HasValue && newPlanTotalPkr.Value > 0
                    ? newPlanTotalPkr.Value
                    : projectedSchedulesTotal;
                targetTotal = Math.Round(targetTotal, 2, MidpointRounding.AwayFromZero);

                if (targetTotal < projectedSchedulesTotal)
                {
                    TempData["Error"] = $"New plan total must be at least PKR {projectedSchedulesTotal:N0} (sum of all installments after this change).";
                    return RedirectToAction(nameof(PaymentSchedule), new { planId = schedule.PlanID });
                }

                newPlanTotalApplied = targetTotal;
            }

            var oldScheduleAmount = existingSchedule.Amount;
            var oldScheduleAmountUsd = existingSchedule.AmountUSD;
            var oldPlanTotal = plan.TotalAmount;
            var oldPlanTotalUsd = plan.TotalAmountUSD;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (newPlanTotalApplied.HasValue)
                {
                    plan.TotalAmount = newPlanTotalApplied.Value;
                    if (exchangeRate > 0)
                        plan.TotalAmountUSD = Math.Round(plan.TotalAmount / exchangeRate, 2, MidpointRounding.AwayFromZero);
                }

                existingSchedule.InstallmentNo = schedule.InstallmentNo;
                existingSchedule.PaymentDescription = schedule.PaymentDescription;
                existingSchedule.DueDate = schedule.DueDate;
                existingSchedule.Amount = schedule.Amount;
                existingSchedule.AmountUSD = schedule.AmountUSD;
                existingSchedule.SurchargeRate = schedule.SurchargeRate;
                existingSchedule.SurchargeApplied = schedule.SurchargeApplied;
                existingSchedule.Description = schedule.Description;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var auditPayload = new
                    {
                        eventType = "PaymentScheduleEdit",
                        timestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                        actorUserId = userId,
                        actorUserName = userName,
                        planId = plan.PlanID,
                        planName = plan.PlanName,
                        scheduleId = schedule.ScheduleID,
                        installmentNo = schedule.InstallmentNo,
                        customersAssignedCount,
                        scheduleAmountPkr = new { from = oldScheduleAmount, to = schedule.Amount },
                        scheduleAmountUsd = new { from = oldScheduleAmountUsd, to = schedule.AmountUSD },
                        planTotalPkr = newPlanTotalApplied.HasValue
                            ? new { from = oldPlanTotal, to = plan.TotalAmount }
                            : new { from = oldPlanTotal, to = oldPlanTotal },
                        planTotalUsd = newPlanTotalApplied.HasValue
                            ? new { from = oldPlanTotalUsd, to = plan.TotalAmountUSD }
                            : new { from = oldPlanTotalUsd, to = oldPlanTotalUsd },
                        projectedSchedulesTotalPkrAfterEdit = projectedSchedulesTotal,
                        planTotalAdjusted = newPlanTotalApplied.HasValue,
                        changeReason = changeReason?.Trim()
                    };
                    var detailsJson = JsonSerializer.Serialize(auditPayload, jsonOptions);
                    var actionSummary = newPlanTotalApplied.HasValue
                        ? $"Installment updated; plan total PKR {oldPlanTotal:N0}→{plan.TotalAmount:N0} ({customersAssignedCount} customers on plan)"
                        : $"Installment updated (schedule {schedule.ScheduleID})";
                    await LogActivityAsync(userId, actionSummary, "PaymentSchedule", schedule.ScheduleID, detailsJson, saveImmediately: false);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Success"] = newPlanTotalApplied.HasValue
                    ? $"Installment saved. Payment plan total increased to PKR {plan.TotalAmount:N0}. {customersAssignedCount} customer(s) remain on this plan."
                    : "Installment updated successfully.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"An error occurred while updating the installment: {ex.Message}";
            }

            return RedirectToAction(nameof(PaymentSchedule), new { planId = schedule.PlanID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePaymentSchedule(string scheduleId, string planId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            var schedule = await _context.PaymentSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                _context.PaymentSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivityAsync(userId, "Delete Payment Schedule", "PaymentSchedule", scheduleId);
                }

                TempData["Success"] = "Installment deleted successfully.";
            }

            return RedirectToAction(nameof(PaymentSchedule), new { planId });
        }

        [HttpGet]
        public async Task<IActionResult> Penalties()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var penalties = await _context.Penalties
                .Include(p => p.Customer)
                .ToListAsync();
            
            ViewBag.Customers = await _context.Customers.ToListAsync();
            return View(penalties);
        }

        [HttpPost]
        public async Task<IActionResult> AddPenalty(string customerId, decimal amount, string reason)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(customerId) || amount <= 0)
            {
                return BadRequest();
            }

            var penalty = new Penalty
            {
                PenaltyID = GenerateID(),
                CustomerID = customerId,
                Amount = amount,
                Reason = reason,
                AppliedOn = DateTime.Now
            };

            _context.Penalties.Add(penalty);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivityAsync(userId, "Add Penalty", "Penalty", penalty.PenaltyID);
            }

            return RedirectToAction(nameof(Penalties));
        }

        [HttpGet]
        public async Task<IActionResult> Waivers()
        {
            return RedirectToAction("Index", "Waiver");
        }

        [HttpPost]
        public async Task<IActionResult> AddWaiver(string customerId, decimal amount, string reason)
        {
            return RedirectToAction("Create", "Waiver", new { customerId });
        }

        private static string TruncateForLog(string? value, int maxLen)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLen ? value : value[..maxLen];
        }

        private static string SanitizePathSegment(string? segment)
        {
            var s = (segment ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            s = s.Replace(Path.DirectorySeparatorChar, '_')
                 .Replace(Path.AltDirectorySeparatorChar, '_')
                 .Trim()
                 .TrimEnd('.');

            return s;
        }

        /// <param name="saveImmediately">When false, caller must call <c>SaveChangesAsync</c> (e.g. within a transaction).</param>
        private async Task LogActivityAsync(string? userId, string action, string refType, string refId, string? details = null, bool saveImmediately = true)
        {
            var activityLog = new ActivityLog
            {
                UserID = string.IsNullOrEmpty(userId) ? null : TruncateForLog(userId, 10),
                Action = TruncateForLog(action, 255),
                RefType = TruncateForLog(refType, 50),
                RefID = TruncateForLog(refId, 10),
                Details = details,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(activityLog);
            if (saveImmediately)
                await _context.SaveChangesAsync();
        }

        private async Task<string?> SavePaymentAttachmentAsync(string paymentId, IFormFile file, string? description, string? uploadedByRaw)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedPaymentAttachmentExtensions.Contains(ext))
            {
                return "Only image files (JPG, PNG, GIF, BMP) and PDF are allowed for payment proof.";
            }
            if (file.Length > MaxPaymentAttachmentSize)
            {
                return "Payment proof file size exceeds 8MB limit.";
            }

            var safeId = SanitizePathSegment(paymentId);
            if (string.IsNullOrWhiteSpace(safeId))
            {
                return "Invalid payment ID for file storage.";
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payments", safeId);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var relativePath = $"/uploads/payments/{safeId}/{uniqueFileName}";

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var uploadedBy = string.IsNullOrEmpty(uploadedByRaw)
                ? null
                : (uploadedByRaw.Length <= 10 ? uploadedByRaw : uploadedByRaw[..10]);

            var attachment = new Attachment
            {
                AttachmentID = GenerateID(),
                RefType = "Payment",
                RefID = paymentId,
                AttachmentType = "Proof",
                FileName = file.FileName,
                FilePath = relativePath,
                FileSize = file.Length,
                FileType = file.ContentType,
                Description = string.IsNullOrWhiteSpace(description) ? "Payment proof" : description.Trim(),
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();
            return null;
        }

        private string GenerateID()
        {
            return Guid.NewGuid().ToString("N")[..10].ToUpper();
        }
    }
}
