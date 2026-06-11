using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;

namespace PMS.Controllers
{
    [Authorize]
    public class RentalController : Controller
    {
        private const string ModuleKey = "Rental";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public RentalController(PMSDbContext context, IModulePermissionService modulePermission)
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

        public async Task<IActionResult> Index(string propertyIdFilter = "", string statusFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var query = _context.Rentals
                .Include(r => r.Property)
                    .ThenInclude(p => p!.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(propertyIdFilter))
                query = query.Where(r => r.PropertyID.Contains(propertyIdFilter));

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(r => r.Status == statusFilter);

            var list = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.PropertyIdFilter = propertyIdFilter ?? "";
            ViewBag.StatusFilter = statusFilter ?? "";
            ViewBag.Statuses = new[] { Rental.StatusActive, Rental.StatusCompleted, Rental.StatusCancelled };
            ViewBag.ActiveCount = await _context.Rentals.CountAsync(r => r.Status == Rental.StatusActive);
            ViewBag.CompletedCount = await _context.Rentals.CountAsync(r => r.Status == Rental.StatusCompleted);
            ViewBag.CancelledCount = await _context.Rentals.CountAsync(r => r.Status == Rental.StatusCancelled);
            return View(list);
        }

        public async Task<IActionResult> Create(string? propertyId = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var model = new Rental
            {
                Status = Rental.StatusActive,
                Currency = "PKR",
                StartDate = DateTime.Today,
                DurationMonths = 12,
                PaymentDueDayOfMonth = 5
            };

            if (!string.IsNullOrWhiteSpace(propertyId))
                model.PropertyID = propertyId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPropertyForRental(string propertyId)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return Json(new { success = false, message = "Property ID is required." });

            var prop = await _context.Properties
                .Include(p => p.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PropertyID == propertyId);

            if (prop == null)
                return Json(new { success = false, message = "Property not found." });

            if (!string.Equals(prop.Status, "Available", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = $"Property is not available (current status: {prop.Status})." });

            // Guard: ensure no active rental already exists
            var hasActiveRental = await _context.Rentals
                .AsNoTracking()
                .AnyAsync(r => r.PropertyID == propertyId && r.Status == Rental.StatusActive);
            if (hasActiveRental)
                return Json(new { success = false, message = "This property already has an active rental." });

            return Json(new
            {
                success = true,
                propertyID = prop.PropertyID,
                plotNo = prop.PlotNo,
                street = prop.Street,
                block = prop.Block,
                plotType = prop.PlotType,
                propertyType = prop.PropertyType,
                size = prop.Size,
                status = prop.Status,
                projectName = prop.Project?.ProjectName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rental model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            // Property must exist and be available
            var prop = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyID == model.PropertyID);
            if (prop == null)
                ModelState.AddModelError("PropertyID", "Property ID not found.");
            else if (!string.Equals(prop.Status, "Available", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("PropertyID", $"Property is not available (current status: {prop.Status}).");

            if (model.DurationMonths <= 0)
                ModelState.AddModelError("DurationMonths", "Duration must be greater than zero.");

            if (model.MonthlyRent <= 0)
                ModelState.AddModelError("MonthlyRent", "Monthly rent must be greater than zero.");

            if (model.PaymentDueDayOfMonth.HasValue && (model.PaymentDueDayOfMonth < 1 || model.PaymentDueDayOfMonth > 28))
                ModelState.AddModelError("PaymentDueDayOfMonth", "Due day must be between 1 and 28 (recommended).");

            var hasActiveRental = await _context.Rentals
                .AnyAsync(r => r.PropertyID == model.PropertyID && r.Status == Rental.StatusActive);
            if (hasActiveRental)
                ModelState.AddModelError("PropertyID", "This property already has an active rental.");

            if (!ModelState.IsValid)
                return View(model);

            model.RentalID = await GenerateRentalIdAsync();
            model.Status = Rental.StatusActive;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = null;
            model.EndDate = CalculateEndDate(model.StartDate, model.DurationMonths);

            _context.Rentals.Add(model);

            // Update property status to Rented
            if (prop != null)
            {
                prop.Status = "Rented";
                _context.Properties.Update(prop);
            }

            // Generate rental payment schedule (monthly)
            var schedule = GenerateMonthlySchedule(model);
            _context.RentalPayments.AddRange(schedule);

            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Create Rental", "Rental", model.RentalID);
            }

            TempData["Success"] = "Rental created successfully and monthly payment schedule generated.";
            return RedirectToAction(nameof(Details), new { id = model.RentalID });
        }

        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var rental = await _context.Rentals
                .Include(r => r.Property)
                    .ThenInclude(p => p!.Project)
                .Include(r => r.RentalPayments)
                .FirstOrDefaultAsync(r => r.RentalID == id);

            if (rental == null)
                return NotFound();

            return View(rental);
        }

        /// <summary>Opens a print-friendly rental payment receipt in a new tab.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintReceipt(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var payment = await _context.RentalPayments
                .Include(p => p.Rental)
                    .ThenInclude(r => r!.Property)
                        .ThenInclude(pr => pr!.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.RentalPaymentID == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> RecordPayment(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var payment = await _context.RentalPayments
                .Include(p => p.Rental)
                    .ThenInclude(r => r!.Property)
                        .ThenInclude(pr => pr!.Project)
                .FirstOrDefaultAsync(p => p.RentalPaymentID == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(string rentalPaymentId, decimal amountPaid, string paymentMethod, string referenceNo, string remarks)
        {
            if (string.IsNullOrWhiteSpace(rentalPaymentId))
                return NotFound();

            var payment = await _context.RentalPayments
                .Include(p => p.Rental)
                .FirstOrDefaultAsync(p => p.RentalPaymentID == rentalPaymentId);

            if (payment == null)
                return NotFound();

            if (amountPaid <= 0)
            {
                TempData["Error"] = "Amount paid must be greater than zero.";
                return RedirectToAction(nameof(RecordPayment), new { id = rentalPaymentId });
            }

            payment.AmountPaid = amountPaid;
            payment.PaidOn = DateTime.Now;
            payment.PaymentMethod = paymentMethod;
            payment.ReferenceNo = referenceNo;
            payment.Remarks = remarks;
            payment.Status = amountPaid >= payment.AmountDue ? RentalPayment.StatusPaid : RentalPayment.StatusPartiallyPaid;

            if (payment.Rental != null)
            {
                payment.Rental.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Record Rental Payment", "RentalPayment", payment.RentalPaymentID);
            }

            TempData["Success"] = "Payment recorded successfully.";
            return RedirectToAction(nameof(Details), new { id = payment.RentalID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseRental(string id, string closeStatus = Rental.StatusCompleted)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var rental = await _context.Rentals
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.RentalID == id);

            if (rental == null)
                return NotFound();

            if (rental.Status != Rental.StatusActive)
            {
                TempData["Error"] = "Only active rentals can be closed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            rental.Status = closeStatus == Rental.StatusCancelled ? Rental.StatusCancelled : Rental.StatusCompleted;
            rental.UpdatedAt = DateTime.Now;
            rental.EndDate = rental.EndDate ?? DateTime.Today;

            if (rental.Property != null)
            {
                // Only revert if it is still marked as Rented (avoid overriding other states)
                if (string.Equals(rental.Property.Status, "Rented", StringComparison.OrdinalIgnoreCase))
                    rental.Property.Status = "Available";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rental closed successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private static DateTime? CalculateEndDate(DateTime startDate, int durationMonths)
        {
            if (durationMonths <= 0) return null;
            // End date = last day of the final month (inclusive), keeping it simple
            var endMonth = startDate.Date.AddMonths(durationMonths);
            return endMonth.AddDays(-1);
        }

        private List<RentalPayment> GenerateMonthlySchedule(Rental rental)
        {
            var items = new List<RentalPayment>();
            var dueDay = rental.PaymentDueDayOfMonth ?? rental.StartDate.Day;
            dueDay = Math.Clamp(dueDay, 1, 28);

            for (int i = 0; i < rental.DurationMonths; i++)
            {
                var monthStart = rental.StartDate.Date.AddMonths(i);
                var dueDate = new DateTime(monthStart.Year, monthStart.Month, Math.Min(dueDay, DateTime.DaysInMonth(monthStart.Year, monthStart.Month)));

                items.Add(new RentalPayment
                {
                    RentalPaymentID = GenerateId50("RNP"),
                    RentalID = rental.RentalID,
                    BillingYear = dueDate.Year,
                    BillingMonth = dueDate.Month,
                    DueDate = dueDate,
                    AmountDue = rental.MonthlyRent,
                    AmountPaid = 0m,
                    Status = RentalPayment.StatusPending,
                    CreatedAt = DateTime.Now
                });
            }

            return items;
        }

        private async Task<string> GenerateRentalIdAsync()
        {
            var prefix = "RNT-";
            var today = DateTime.Today.ToString("yyyyMMdd");
            var existing = await _context.Rentals
                .Where(r => r.RentalID.StartsWith(prefix + today))
                .OrderByDescending(r => r.RentalID)
                .Select(r => r.RentalID)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrEmpty(existing) && existing.Length >= prefix.Length + today.Length + 2)
            {
                var part = existing[(prefix.Length + today.Length)..].TrimStart('-');
                if (int.TryParse(part, out var n)) seq = n + 1;
            }
            return prefix + today + "-" + seq.ToString("D4");
        }

        private static string GenerateId50(string prefix)
        {
            // 50 chars max; keep human-friendly
            return $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(50, (prefix.Length + 1 + 32)));
        }

        private async Task LogActivity(string userId, string action, string refType, string refId)
        {
            var activityLog = new ActivityLog
            {
                UserID = userId,
                Action = action,
                RefType = refType,
                RefID = refId.Length <= 10 ? refId : refId[..10],
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
    }
}

