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
    public class PaymentAuditController : Controller
    {
        private const string ModuleKey = "PaymentAudit";

        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public PaymentAuditController(PMSDbContext context, IModulePermissionService modulePermission)
        {
            _context = context;
            _modulePermission = modulePermission;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");
            var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
            var levels = new[] { "Read", "Edit", "Admin" };
            var editLevels = new[] { "Edit", "Admin" };
            bool allowed = requiredLevel == "Read"
                ? levels.Contains(perm)
                : editLevels.Contains(perm);
            if (!allowed) return RedirectToAction("AccessDenied", "Account");

            ViewBag.CanEdit = editLevels.Contains(perm);
            ViewBag.CanAdmin = perm == "Admin";
            return null;
        }

        // GET: /PaymentAudit
        public async Task<IActionResult> Index(string auditFilter = "", string customerFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var query = _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentSchedule)
                .Include(p => p.AuditedByUser)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(auditFilter))
                query = query.Where(p => p.AuditStatus == auditFilter);

            if (!string.IsNullOrWhiteSpace(customerFilter))
                query = query.Where(p => p.CustomerID == customerFilter.Trim());

            var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

            ViewBag.AuditFilter = auditFilter;
            ViewBag.CustomerFilter = customerFilter;

            return View(payments);
        }

        // GET: /PaymentAudit/Audit/{id}
        public async Task<IActionResult> Audit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrEmpty(id)) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentSchedule)
                    .ThenInclude(s => s!.PaymentPlan)
                        .ThenInclude(pl => pl!.Project)
                .Include(p => p.AuditedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null) return NotFound();

            ViewBag.PaymentAttachments = await _context.Attachments
                .AsNoTracking()
                .Where(a => a.RefType == "Payment" && a.RefID == id)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            return View(payment);
        }

        // POST: /PaymentAudit/Audit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Audit(string paymentId, string auditStatus, string auditRemarks)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(paymentId))
            {
                TempData["ErrorMessage"] = "Payment ID is required.";
                return RedirectToAction(nameof(Index));
            }

            if (auditStatus != "Approved" && auditStatus != "Declined")
            {
                TempData["ErrorMessage"] = "Invalid audit status. Must be Approved or Declined.";
                return RedirectToAction(nameof(Audit), new { id = paymentId });
            }

            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment not found.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            payment.AuditStatus = auditStatus;
            payment.AuditedBy = userId;
            payment.AuditedAt = DateTime.Now;
            payment.AuditRemarks = auditRemarks?.Trim();

            _context.Payments.Update(payment);

            // Log activity
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID = userId,
                Action = $"Payment {paymentId} audit: {auditStatus}. Remarks: {(string.IsNullOrWhiteSpace(auditRemarks) ? "—" : auditRemarks)}",
                RefType = "Payment",
                RefID = paymentId,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Payment {paymentId} has been {auditStatus.ToLower()}.";
            return RedirectToAction(nameof(Index), new { auditFilter = "" });
        }

        // POST: reset audit status back to Pending
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAudit(string paymentId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment not found.";
                return RedirectToAction(nameof(Index));
            }

            payment.AuditStatus = "Pending";
            payment.AuditedBy = null;
            payment.AuditedAt = null;
            payment.AuditRemarks = null;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Audit status reset to Pending for payment {paymentId}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
