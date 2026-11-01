using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Salzee.Controllers
{
    [Authorize]
    public class RefundController : Controller
    {
        private const string ModuleKey = "Refund";
        private const string RefundedStatus = "Refunded";

        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private readonly IAmsPmsIntegrationService _amsPmsIntegration;

        public RefundController(PMSDbContext context, IModulePermissionService modulePermission, IAmsPmsIntegrationService amsPmsIntegration)
        {
            _context = context;
            _modulePermission = modulePermission;
            _amsPmsIntegration = amsPmsIntegration;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");
            var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
            if (requiredLevel == "Read"  && !_modulePermission.CanRead(perm))  return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Edit"  && !_modulePermission.CanEdit(perm))  return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Admin" && !_modulePermission.CanDelete(perm)) return RedirectToAction("AccessDenied", "Account");
            ViewBag.CanCreate = _modulePermission.CanEdit(perm);
            ViewBag.CanEdit   = _modulePermission.CanEdit(perm);
            ViewBag.CanDelete = _modulePermission.CanDelete(perm);
            return null;
        }

        private async Task<List<string>> GetWorkflowStatusesAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "refundworkflow");
            return config?.ConfigValue != null
                ? config.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string> { "Initiated", "Approved", "Declined" };
        }

        private async Task SetWorkflowStatusesViewBagAsync()
        {
            var statuses = await GetWorkflowStatusesAsync();
            ViewBag.WorkflowStatuses = statuses;
            ViewBag.InitiatedStatus = ResolveWorkflowStatus(statuses, "Initiated");
            ViewBag.ApprovedStatus = ResolveWorkflowStatus(statuses, "Approved");
            ViewBag.DeclinedStatus = ResolveWorkflowStatus(statuses, "Declined");
        }

        private static string ResolveWorkflowStatus(IEnumerable<string> statuses, string fallback)
        {
            return statuses.FirstOrDefault(s => string.Equals(s, fallback, StringComparison.OrdinalIgnoreCase)) ?? fallback;
        }

        private static int GetWorkflowIndex(List<string> statuses, string? currentStatus)
        {
            if (statuses.Count == 0) return -1;
            if (string.IsNullOrWhiteSpace(currentStatus)) return -1;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (string.Equals(statuses[i], currentStatus, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private async Task ApplyRefundedStateAsync(Refund refund)
        {
            // Mark selected payments as refunded (for audit-friendly count)
            List<string>? selectedIds = null;
            if (!string.IsNullOrEmpty(refund.SelectedPaymentIDs))
            {
                try { selectedIds = JsonSerializer.Deserialize<List<string>>(refund.SelectedPaymentIDs); }
                catch { }
            }

            if (selectedIds != null && selectedIds.Count > 0)
            {
                var selectedPayments = await _context.Payments
                    .Where(p => selectedIds.Contains(p.PaymentID))
                    .ToListAsync();

                foreach (var payment in selectedPayments)
                {
                    payment.Status = RefundedStatus;
                }
            }

            // Mark all customer payments as refunded
            var allCustomerPayments = await _context.Payments
                .Where(p => p.CustomerID == refund.CustomerID)
                .ToListAsync();
            foreach (var payment in allCustomerPayments)
            {
                payment.Status = RefundedStatus;
            }

            // Mark customer status as refunded
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == refund.CustomerID);
            if (customer != null)
            {
                customer.Status = RefundedStatus;
            }
        }

        private async Task<string> GenerateRefundIdAsync()
        {
            var last = await _context.Refunds
                .OrderByDescending(r => r.RefundID)
                .Select(r => r.RefundID)
                .FirstOrDefaultAsync();
            int next = 1;
            if (!string.IsNullOrEmpty(last))
            {
                var numPart = last.TrimStart('R', 'F', 'D');
                if (int.TryParse(numPart, out int n)) next = n + 1;
            }
            return "RFD" + next.ToString("D7");
        }

        // GET: /Refund
        public async Task<IActionResult> Index(string workflowFilter = "", string customerFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var query = _context.Refunds
                .Include(r => r.Customer)
                .Include(r => r.CreatedByUser)
                .Include(r => r.ApprovedByUser)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(workflowFilter))
            {
                if (string.Equals(workflowFilter, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        !string.Equals(r.WorkflowStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(r.WorkflowStatus, "Declined", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    query = query.Where(r => r.WorkflowStatus == workflowFilter);
                }
            }
            if (!string.IsNullOrWhiteSpace(customerFilter))
                query = query.Where(r => r.CustomerID == customerFilter.Trim());

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            await SetWorkflowStatusesViewBagAsync();
            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Approved", "Declined" };
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");
            var declinedStatus = ResolveWorkflowStatus(workflowStatuses, "Declined");
            ViewBag.WorkflowFilter  = workflowFilter;
            ViewBag.CustomerFilter  = customerFilter;
            ViewBag.InitiatedCount  = await _context.Refunds.CountAsync(r => r.WorkflowStatus == initiatedStatus);
            ViewBag.ApprovedCount   = await _context.Refunds.CountAsync(r => r.WorkflowStatus == approvedStatus);
            ViewBag.DeclinedCount   = await _context.Refunds.CountAsync(r => r.WorkflowStatus == declinedStatus);
            return View(list);
        }

        // GET: /Refund/Create
        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();
            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Approved", "Declined" };
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            return View(new Refund { WorkflowStatus = initiatedStatus, CreatedAt = DateTime.Now });
        }

        // AJAX POST: search customer and return their outstanding payments
        [HttpPost]
        public async Task<IActionResult> SearchCustomerForRefund(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return Json(new { success = false, message = "Enter a Customer ID." });

            var customer = await _context.Customers
                .Include(c => c.PaymentPlan)
                    .ThenInclude(p => p!.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == customerId.Trim());

            if (customer == null)
                return Json(new { success = false, message = "Customer not found." });

            var workflowStatuses = await GetWorkflowStatusesAsync();
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");

            // Get payments that are not already linked to an approved refund (legacy safeguard).
            var approvedRefundPaymentIds = await _context.Refunds
                .Where(r => r.WorkflowStatus == approvedStatus && r.SelectedPaymentIDs != null)
                .Select(r => r.SelectedPaymentIDs)
                .ToListAsync();

            var alreadyRefundedIds = new HashSet<string>();
            foreach (var json in approvedRefundPaymentIds)
            {
                if (string.IsNullOrEmpty(json)) continue;
                try
                {
                    var ids = JsonSerializer.Deserialize<List<string>>(json);
                    if (ids != null) foreach (var id in ids) alreadyRefundedIds.Add(id);
                }
                catch { }
            }

            var payments = await _context.Payments
                .Include(p => p.PaymentSchedule)
                .Where(p => p.CustomerID == customer.CustomerID && p.Status != RefundedStatus)
                .AsNoTracking()
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            var paymentDtos = payments
                .Where(p => !alreadyRefundedIds.Contains(p.PaymentID))
                .Select(p => new
                {
                    paymentId   = p.PaymentID,
                    description = p.PaymentSchedule?.PaymentDescription ?? "—",
                    installmentNo = p.PaymentSchedule?.InstallmentNo,
                    amount      = p.Amount,
                    method      = p.Method ?? "—",
                    date        = p.PaymentDate.ToString("MMM dd, yyyy"),
                    referenceNo = p.ReferenceNo ?? "—",
                    status      = p.Status ?? "—"
                }).ToList();

            return Json(new
            {
                success     = true,
                customerID  = customer.CustomerID,
                fullName    = customer.FullName ?? "—",
                cnic        = customer.CNIC ?? "—",
                phone       = customer.Phone ?? "—",
                project     = customer.PaymentPlan?.Project?.ProjectName ?? "—",
                payments    = paymentDtos
            });
        }

        // POST: /Refund/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Refund model, string selectedPaymentIdsJson, IFormFile? refundAttachment = null, string? refundAttachmentDescription = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validate customer
            if (string.IsNullOrWhiteSpace(model.CustomerID) ||
                !await _context.Customers.AnyAsync(c => c.CustomerID == model.CustomerID))
            {
                TempData["ErrorMessage"] = "Valid Customer ID is required.";
                await SetWorkflowStatusesViewBagAsync();
                return View(model);
            }

            // Validate payment selection
            List<string>? selectedIds = null;
            if (!string.IsNullOrWhiteSpace(selectedPaymentIdsJson))
            {
                try { selectedIds = JsonSerializer.Deserialize<List<string>>(selectedPaymentIdsJson); }
                catch { }
            }
            if (selectedIds == null || selectedIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select at least one payment to refund.";
                await SetWorkflowStatusesViewBagAsync();
                return View(model);
            }

            // Auto-calculate refunded amount
            model.RefundedAmount = model.PaidAmount - model.DeductionAmount;
            if (model.RefundedAmount < 0) model.RefundedAmount = 0;

            model.RefundID = await GenerateRefundIdAsync();
            var workflowStatuses = await GetWorkflowStatusesAsync();
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            model.WorkflowStatus = initiatedStatus;
            model.CreatedBy = userId;
            model.CreatedAt = DateTime.Now;
            model.SelectedPaymentIDs = JsonSerializer.Serialize(selectedIds);

            _context.Refunds.Add(model);

            if (refundAttachment != null && refundAttachment.Length > 0)
            {
                var attachmentError = await SaveRefundAttachmentAsync(model.RefundID, refundAttachment, refundAttachmentDescription, userId);
                if (!string.IsNullOrEmpty(attachmentError))
                {
                    TempData["ErrorMessage"] = attachmentError;
                    await SetWorkflowStatusesViewBagAsync();
                    return View(model);
                }
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID    = userId,
                Action    = $"Refund {model.RefundID} initiated for customer {model.CustomerID}. Amount: PKR {model.PaidAmount:N0}",
                RefType   = "Refund",
                RefID     = model.RefundID,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Refund {model.RefundID} created successfully.";
            return RedirectToAction(nameof(Details), new { id = model.RefundID });
        }

        // GET: /Refund/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var refund = await _context.Refunds
                .Include(r => r.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .Include(r => r.CreatedByUser)
                .Include(r => r.ApprovedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RefundID == id);

            if (refund == null) return NotFound();

            // Resolve selected payments for display
            List<Payment>? selectedPayments = null;
            if (!string.IsNullOrEmpty(refund.SelectedPaymentIDs))
            {
                try
                {
                    var ids = JsonSerializer.Deserialize<List<string>>(refund.SelectedPaymentIDs);
                    if (ids != null && ids.Count > 0)
                    {
                        selectedPayments = await _context.Payments
                            .Include(p => p.PaymentSchedule)
                            .Where(p => ids.Contains(p.PaymentID))
                            .AsNoTracking()
                            .ToListAsync();
                    }
                }
                catch { }
            }

            ViewBag.SelectedPayments = selectedPayments ?? new List<Payment>();
            return View(refund);
        }

        // GET: /Refund/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var refund = await _context.Refunds
                .Include(r => r.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .Include(r => r.CreatedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RefundID == id);

            if (refund == null) return NotFound();

            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Approved", "Declined" };
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");
            if (string.Equals(refund.WorkflowStatus, approvedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Approved refunds cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentIndex = GetWorkflowIndex(workflowStatuses, refund.WorkflowStatus);
            ViewBag.CurrentWorkflowIndex = currentIndex;
            ViewBag.HasPreviousStep = currentIndex > 0;
            ViewBag.HasNextStep = currentIndex >= 0 && currentIndex < workflowStatuses.Count - 1;
            ViewBag.PreviousStepLabel = currentIndex > 0 ? workflowStatuses[currentIndex - 1] : null;
            ViewBag.NextStepLabel = currentIndex >= 0 && currentIndex < workflowStatuses.Count - 1 ? workflowStatuses[currentIndex + 1] : null;

            var selectedPayments = await GetSelectedPaymentsAsync(refund.SelectedPaymentIDs);
            ViewBag.SelectedPayments = selectedPayments;
            return View(refund);
        }

        // POST: /Refund/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Refund model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var existing = await _context.Refunds.FirstOrDefaultAsync(r => r.RefundID == id);
            if (existing == null) return NotFound();

            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Approved", "Declined" };
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");
            if (string.Equals(existing.WorkflowStatus, approvedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Approved refunds cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(model.RefundType))
                ModelState.AddModelError(nameof(model.RefundType), "Refund Type is required.");
            if (string.IsNullOrWhiteSpace(model.Reason))
                ModelState.AddModelError(nameof(model.Reason), "Reason is required.");
            if (model.DeductionAmount < 0)
                ModelState.AddModelError(nameof(model.DeductionAmount), "Deduction cannot be negative.");

            existing.DeductionAmount = model.DeductionAmount;
            existing.RefundedAmount = existing.PaidAmount - existing.DeductionAmount;
            if (existing.RefundedAmount < 0) existing.RefundedAmount = 0;

            var chequeSum = await _context.RefundCheques
                .Where(c => c.RefundID == id)
                .SumAsync(c => (decimal?)c.Amount) ?? 0m;
            if (chequeSum > existing.RefundedAmount)
            {
                ModelState.AddModelError(nameof(model.DeductionAmount),
                    $"Total cheque amounts (PKR {chequeSum:N2}) exceed the new refunded amount (PKR {existing.RefundedAmount:N2}). Reduce or remove cheques on the Refund cheques tab before increasing the deduction.");
            }

            if (!ModelState.IsValid)
            {
                var selectedPaymentsInvalid = await GetSelectedPaymentsAsync(existing.SelectedPaymentIDs);
                ViewBag.SelectedPayments = selectedPaymentsInvalid;
                model.RefundID = existing.RefundID;
                model.CustomerID = existing.CustomerID;
                model.PaidAmount = existing.PaidAmount;
                model.RefundedAmount = existing.RefundedAmount;
                model.WorkflowStatus = existing.WorkflowStatus;
                model.CreatedAt = existing.CreatedAt;
                model.SelectedPaymentIDs = existing.SelectedPaymentIDs;
                return View(model);
            }

            existing.RefundType = model.RefundType;
            existing.Reason = model.Reason;
            existing.Notes = model.Notes;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Refund {existing.RefundID} edited.",
                    RefType = "Refund",
                    RefID = existing.RefundID,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Refund {existing.RefundID} updated successfully.";
            return RedirectToAction(nameof(Details), new { id = existing.RefundID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveStatus(string id, string direction)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var refund = await _context.Refunds.FirstOrDefaultAsync(r => r.RefundID == id);
            if (refund == null)
            {
                TempData["ErrorMessage"] = "Refund not found.";
                return RedirectToAction(nameof(Index));
            }

            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Approved", "Declined" };
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");

            // Approved is terminal for editing/workflow moves.
            if (string.Equals(refund.WorkflowStatus, approvedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Approved refunds cannot be moved.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var idx = GetWorkflowIndex(workflowStatuses, refund.WorkflowStatus);
            if (idx < 0)
            {
                TempData["ErrorMessage"] = "Current refund status is not part of configured refundworkflow.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var requestedForward = string.Equals(direction, "forward", StringComparison.OrdinalIgnoreCase);
            var requestedBackward = string.Equals(direction, "backward", StringComparison.OrdinalIgnoreCase);
            if (!requestedForward && !requestedBackward)
            {
                TempData["ErrorMessage"] = "Invalid status move direction.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var targetIndex = requestedForward ? idx + 1 : idx - 1;
            if (targetIndex < 0 || targetIndex >= workflowStatuses.Count)
            {
                TempData["ErrorMessage"] = "No further workflow step available in that direction.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var oldStatus = refund.WorkflowStatus ?? "";
            var targetStatus = workflowStatuses[targetIndex];
            refund.WorkflowStatus = targetStatus;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.Equals(targetStatus, approvedStatus, StringComparison.OrdinalIgnoreCase))
            {
                await ApplyRefundedStateAsync(refund);
                refund.ApprovedBy = userId;
                refund.ApprovedAt = DateTime.Now;
            }

            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Refund {refund.RefundID} status changed from {oldStatus} to {targetStatus}.",
                    RefType = "Refund",
                    RefID = refund.RefundID,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Refund {refund.RefundID} moved to {targetStatus}.";
            if (string.Equals(targetStatus, approvedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Details), new { id = refund.RefundID });
            }
            return RedirectToAction(nameof(Edit), new { id = refund.RefundID });
        }

        // POST: /Refund/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var refund = await _context.Refunds.FirstOrDefaultAsync(r => r.RefundID == id);
            if (refund == null)
            {
                TempData["ErrorMessage"] = "Refund not found.";
                return RedirectToAction(nameof(Index));
            }

            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Approved", "Declined" };
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            if (!string.Equals(refund.WorkflowStatus, initiatedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only initiated refunds can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _context.Refunds.Remove(refund);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Refund {refund.RefundID} deleted.",
                    RefType = "Refund",
                    RefID = refund.RefundID,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Refund {refund.RefundID} deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Refund/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string refundId, string? notes)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            var refund = await _context.Refunds.FindAsync(refundId);
            if (refund == null)
            {
                TempData["ErrorMessage"] = "Refund not found.";
                return RedirectToAction(nameof(Index));
            }
            var workflowStatuses = await GetWorkflowStatusesAsync();
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");
            if (!string.Equals(refund.WorkflowStatus, initiatedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Cannot approve a refund that is already {refund.WorkflowStatus}.";
                return RedirectToAction(nameof(Details), new { id = refundId });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Mark selected payments as refunded
            List<string>? selectedIds = null;
            if (!string.IsNullOrEmpty(refund.SelectedPaymentIDs))
            {
                try { selectedIds = JsonSerializer.Deserialize<List<string>>(refund.SelectedPaymentIDs); }
                catch { }
            }

            var updatedSelectedPaymentsCount = 0;
            if (selectedIds != null && selectedIds.Count > 0)
            {
                var selectedPayments = await _context.Payments
                    .Where(p => selectedIds.Contains(p.PaymentID))
                    .ToListAsync();

                foreach (var payment in selectedPayments)
                {
                    payment.Status = RefundedStatus;
                    updatedSelectedPaymentsCount++;
                }
            }

            // Mark all customer payments as refunded
            var allCustomerPayments = await _context.Payments
                .Where(p => p.CustomerID == refund.CustomerID)
                .ToListAsync();
            foreach (var payment in allCustomerPayments)
            {
                payment.Status = RefundedStatus;
            }

            // Mark customer status as refunded
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == refund.CustomerID);
            if (customer != null)
            {
                customer.Status = RefundedStatus;
            }

            // Update refund
            refund.WorkflowStatus = approvedStatus;
            refund.ApprovedBy     = userId;
            refund.ApprovedAt     = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(notes)) refund.Notes = notes;
            _context.Refunds.Update(refund);

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID    = userId,
                Action    = $"Refund {refundId} approved. Selected payments refunded: {updatedSelectedPaymentsCount}. All customer payments and customer status marked as Refunded.",
                RefType   = "Refund",
                RefID     = refundId,
                CreatedAt = DateTime.Now
            });

            await _amsPmsIntegration.TryCreateRefundVoucherOnApprovalAsync(refund, userId);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Refund {refundId} approved. All payments for customer {refund.CustomerID} are now marked as Refunded, and customer status is Refunded.";
            return RedirectToAction(nameof(Details), new { id = refundId });
        }

        // POST: /Refund/Decline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(string refundId, string? notes)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var refund = await _context.Refunds.FindAsync(refundId);
            if (refund == null)
            {
                TempData["ErrorMessage"] = "Refund not found.";
                return RedirectToAction(nameof(Index));
            }
            var workflowStatuses = await GetWorkflowStatusesAsync();
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            var declinedStatus = ResolveWorkflowStatus(workflowStatuses, "Declined");
            if (!string.Equals(refund.WorkflowStatus, initiatedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Cannot decline a refund that is already {refund.WorkflowStatus}.";
                return RedirectToAction(nameof(Details), new { id = refundId });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            refund.WorkflowStatus = declinedStatus;
            refund.ApprovedBy     = userId;
            refund.ApprovedAt     = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(notes)) refund.Notes = notes;
            _context.Refunds.Update(refund);

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID    = userId,
                Action    = $"Refund {refundId} declined.",
                RefType   = "Refund",
                RefID     = refundId,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Refund {refundId} has been declined.";
            return RedirectToAction(nameof(Details), new { id = refundId });
        }

        // ========== Attachment management (same pattern as Transfer) ==========
        private static readonly string[] AllowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private const long MaxAttachmentSize = 8 * 1024 * 1024; // 8MB

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(string refundId, IFormFile file, string description = "")
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            try
            {
                var normalizedRefundId = NormalizeId(refundId);
                if (string.IsNullOrEmpty(normalizedRefundId))
                    return Json(new { success = false, message = "Refund ID is required." });
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Please select a file to upload." });

                var refund = await _context.Refunds.FindAsync(normalizedRefundId);
                if (refund == null)
                    return Json(new { success = false, message = "Refund not found." });
                var uploadedByRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var attachmentError = await SaveRefundAttachmentAsync(normalizedRefundId, file, description, uploadedByRaw);
                if (!string.IsNullOrEmpty(attachmentError))
                    return Json(new { success = false, message = attachmentError });
                await _context.SaveChangesAsync();

                var attachment = await _context.Attachments
                    .Where(a => a.RefType == "Refund" && a.RefID == normalizedRefundId)
                    .OrderByDescending(a => a.UploadedAt)
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    message = "File uploaded successfully",
                    attachment = attachment == null ? null : new
                    {
                        attachmentID = attachment.AttachmentID,
                        fileName = attachment.FileName,
                        filePath = attachment.FilePath,
                        fileSize = attachment.FileSize,
                        fileType = attachment.FileType,
                        attachmentType = attachment.AttachmentType,
                        description = attachment.Description,
                        uploadedAt = attachment.UploadedAt.ToString("MMM dd, yyyy hh:mm tt")
                    }
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " " + ex.InnerException.Message;
                return Json(new { success = false, message = msg });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttachments(string refundId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            try
            {
                var normalizedRefundId = NormalizeId(refundId);
                if (string.IsNullOrEmpty(normalizedRefundId))
                    return Json(new { success = false, message = "Refund ID is required." });

                var attachments = await _context.Attachments
                    .Where(a => a.RefType == "Refund" && a.RefID == normalizedRefundId)
                    .OrderByDescending(a => a.UploadedAt)
                    .Select(a => new
                    {
                        attachmentID = a.AttachmentID,
                        fileName = a.FileName,
                        filePath = a.FilePath,
                        fileSize = a.FileSize,
                        fileType = a.FileType,
                        attachmentType = a.AttachmentType,
                        description = a.Description,
                        uploadedAt = a.UploadedAt.ToString("MMM dd, yyyy hh:mm tt")
                    })
                    .ToListAsync();

                return Json(new { success = true, attachments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(string attachmentId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            try
            {
                if (string.IsNullOrEmpty(attachmentId))
                    return Json(new { success = false, message = "Attachment ID is required." });

                var attachment = await _context.Attachments
                    .FirstOrDefaultAsync(a => a.AttachmentID == attachmentId && a.RefType == "Refund");
                if (attachment == null)
                    return Json(new { success = false, message = "Attachment not found." });

                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    var trimmedPath = attachment.FilePath.TrimStart('~').TrimStart('/');
                    var fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        trimmedPath.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
                _context.Attachments.Remove(attachment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Attachment deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static string NormalizeId(string? id)
        {
            return (id ?? string.Empty).Trim();
        }

        private async Task<string?> SaveRefundAttachmentAsync(string refundId, IFormFile file, string? description, string? uploadedByRaw)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedAttachmentExtensions.Contains(ext))
                return "Only image files (JPG, PNG, GIF, BMP) and PDF are allowed.";
            if (file.Length > MaxAttachmentSize)
                return "File size exceeds 8MB limit.";

            var safeId = SanitizePathSegment(refundId);
            if (string.IsNullOrEmpty(safeId))
                return "Invalid Refund ID for file storage.";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "refunds", safeId);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var relativePath = $"/uploads/refunds/{safeId}/{uniqueFileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var attachmentId = Guid.NewGuid().ToString("N")[..10].ToUpper();
            var uploadedBy = string.IsNullOrEmpty(uploadedByRaw) ? null
                : (uploadedByRaw.Length <= 10 ? uploadedByRaw : uploadedByRaw[..10]);

            var attachment = new Attachment
            {
                AttachmentID = attachmentId,
                RefType = "Refund",
                RefID = refundId,
                AttachmentType = "Refund",
                FileName = file.FileName,
                FilePath = relativePath,
                FileSize = file.Length,
                FileType = file.ContentType,
                Description = description,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            };
            _context.Attachments.Add(attachment);
            return null;
        }

        private static string SanitizePathSegment(string? segment)
        {
            var s = NormalizeId(segment);
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            s = s.Replace(Path.DirectorySeparatorChar, '_')
                 .Replace(Path.AltDirectorySeparatorChar, '_');

            s = s.Trim().TrimEnd('.');
            return s;
        }

        private async Task<List<Payment>> GetSelectedPaymentsAsync(string? selectedPaymentIdsJson)
        {
            if (string.IsNullOrEmpty(selectedPaymentIdsJson))
                return new List<Payment>();

            try
            {
                var ids = JsonSerializer.Deserialize<List<string>>(selectedPaymentIdsJson);
                if (ids == null || ids.Count == 0)
                    return new List<Payment>();

                return await _context.Payments
                    .Include(p => p.PaymentSchedule)
                    .Where(p => ids.Contains(p.PaymentID))
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch
            {
                return new List<Payment>();
            }
        }

        private static string? TruncateUserId(string? userId) =>
            string.IsNullOrEmpty(userId) ? null : (userId.Length <= 10 ? userId : userId[..10]);

        private async Task<IActionResult?> EnsureRefundChequeManageAsync(string refundId)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var workflowStatuses = await GetWorkflowStatusesAsync();
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");
            var refund = await _context.Refunds.AsNoTracking().FirstOrDefaultAsync(r => r.RefundID == refundId);
            if (refund == null)
                return Json(new { success = false, message = "Refund not found." });
            if (string.Equals(refund.WorkflowStatus, approvedStatus, StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Cheques cannot be changed after the refund is approved." });
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetRefundCheques(string refundId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var id = NormalizeId(refundId);
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Refund ID is required." });

            var refund = await _context.Refunds.AsNoTracking().FirstOrDefaultAsync(r => r.RefundID == id);
            if (refund == null)
                return Json(new { success = false, message = "Refund not found." });

            var items = await _context.RefundCheques
                .Where(c => c.RefundID == id)
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    id = c.Id,
                    chequeNo = c.ChequeNo,
                    chequeDate = c.ChequeDate.ToString("yyyy-MM-dd"),
                    amount = c.Amount,
                    bank = c.Bank,
                    details = c.Details,
                    createdAt = c.CreatedAt.ToString("MMM dd, yyyy HH:mm")
                })
                .ToListAsync();

            var total = await _context.RefundCheques.Where(c => c.RefundID == id).SumAsync(c => (decimal?)c.Amount) ?? 0m;
            return Json(new { success = true, items, cap = refund.RefundedAmount, total });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRefundCheque(string refundId, string chequeNo, DateTime chequeDate, decimal amount, string? bank, string? details)
        {
            var id = NormalizeId(refundId);
            var gate = await EnsureRefundChequeManageAsync(id);
            if (gate != null) return gate;

            chequeNo = (chequeNo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(chequeNo))
                return Json(new { success = false, message = "Cheque number is required." });
            if (amount <= 0)
                return Json(new { success = false, message = "Amount must be greater than zero." });

            var refund = await _context.Refunds.FirstOrDefaultAsync(r => r.RefundID == id);
            if (refund == null)
                return Json(new { success = false, message = "Refund not found." });

            var sumExisting = await _context.RefundCheques.Where(c => c.RefundID == id).SumAsync(c => (decimal?)c.Amount) ?? 0m;
            if (sumExisting + amount > refund.RefundedAmount)
                return Json(new { success = false, message = $"Total cheque amounts cannot exceed refunded amount (PKR {refund.RefundedAmount:N2}). Current cheques: PKR {sumExisting:N2}." });

            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            _context.RefundCheques.Add(new RefundCheque
            {
                RefundID = id,
                ChequeNo = chequeNo,
                ChequeDate = chequeDate.Date,
                Amount = amount,
                Bank = string.IsNullOrWhiteSpace(bank) ? null : bank.Trim(),
                Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = userId
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cheque added." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefundCheque(int id, string chequeNo, DateTime chequeDate, decimal amount, string? bank, string? details)
        {
            chequeNo = (chequeNo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(chequeNo))
                return Json(new { success = false, message = "Cheque number is required." });
            if (amount <= 0)
                return Json(new { success = false, message = "Amount must be greater than zero." });

            var row = await _context.RefundCheques.FirstOrDefaultAsync(c => c.Id == id);
            if (row == null)
                return Json(new { success = false, message = "Cheque not found." });

            var gate = await EnsureRefundChequeManageAsync(row.RefundID);
            if (gate != null) return gate;

            var refund = await _context.Refunds.FirstOrDefaultAsync(r => r.RefundID == row.RefundID);
            if (refund == null)
                return Json(new { success = false, message = "Refund not found." });

            var sumOthers = await _context.RefundCheques
                .Where(c => c.RefundID == row.RefundID && c.Id != id)
                .SumAsync(c => (decimal?)c.Amount) ?? 0m;
            if (sumOthers + amount > refund.RefundedAmount)
                return Json(new { success = false, message = $"Total cheque amounts cannot exceed refunded amount (PKR {refund.RefundedAmount:N2}). Other cheques: PKR {sumOthers:N2}." });

            row.ChequeNo = chequeNo;
            row.ChequeDate = chequeDate.Date;
            row.Amount = amount;
            row.Bank = string.IsNullOrWhiteSpace(bank) ? null : bank.Trim();
            row.Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
            row.ModifiedBy = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cheque updated." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRefundCheque(int id)
        {
            var row = await _context.RefundCheques.FirstOrDefaultAsync(c => c.Id == id);
            if (row == null)
                return Json(new { success = false, message = "Cheque not found." });

            var gate = await EnsureRefundChequeManageAsync(row.RefundID);
            if (gate != null) return gate;

            _context.RefundCheques.Remove(row);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cheque removed." });
        }
    }
}
