using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;
using System.Text.RegularExpressions;
using System.IO;

namespace Salzee.Controllers
{
    [Authorize]
    public class NDCController : Controller
    {
        private const string ModuleKey = "NDC";
        private static readonly string[] AllowedNdcAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private const long MaxNdcAttachmentSize = 8 * 1024 * 1024; // 8MB
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private readonly ISurchargeService _surchargeService;

        public NDCController(
            PMSDbContext context,
            IModulePermissionService modulePermission,
            ISurchargeService surchargeService)
        {
            _context = context;
            _modulePermission = modulePermission;
            _surchargeService = surchargeService;
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

        private async Task<string[]> GetNDCWorkflowStatusesAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "NDCWorkFlowStatus");
            if (config?.ConfigValue != null)
            {
                var list = config.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (list.Length > 0) return list;
            }
            return new[] { "Initiated", "Approved", "Declined" };
        }

        private async Task<int> GetNDCExpiryDaysAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "NDCExpiry");
            if (config?.ConfigValue != null && int.TryParse(config.ConfigValue.Trim(), out var days) && days > 0)
                return days;
            return 14;
        }

        private async Task<int> GetNDCStartNormalDaysAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "NDCStartNormal");
            if (config?.ConfigValue != null && int.TryParse(config.ConfigValue.Trim(), out var days) && days >= 0)
                return days;
            return 3;
        }

        private async Task<int> GetNDCStartUrgentDaysAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "NDCStartUrgent");
            if (config?.ConfigValue != null && int.TryParse(config.ConfigValue.Trim(), out var days) && days >= 0)
                return days;
            return 0;
        }

        private async Task<string[]> GetNDCTypesAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "NDCType");
            if (config?.ConfigValue != null)
            {
                var list = config.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (list.Length > 0) return list;
            }
            return new[] { "Normal Transfer", "Urgent Transfer", "Family Transfer", "Death Transfer" };
        }

        private async Task SetNDCConfigViewBagAsync()
        {
            ViewBag.WorkflowStatuses = await GetNDCWorkflowStatusesAsync();
            ViewBag.NDCTypes = await GetNDCTypesAsync();
            ViewBag.NDCExpiryDays = await GetNDCExpiryDaysAsync();
            ViewBag.NDCStartNormalDays = await GetNDCStartNormalDaysAsync();
            ViewBag.NDCStartUrgentDays = await GetNDCStartUrgentDaysAsync();
        }

        public async Task<IActionResult> Index(string customerIdFilter = "", string workflowFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var query = _context.NDCs
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerIdFilter))
                query = query.Where(n => n.CustomerID != null && n.CustomerID.Contains(customerIdFilter));

            if (!string.IsNullOrWhiteSpace(workflowFilter))
                query = query.Where(n => n.WorkFlowStatus == workflowFilter);

            var list = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();

            ViewBag.CustomerIdFilter = customerIdFilter ?? "";
            ViewBag.WorkflowFilter = workflowFilter ?? "";
            ViewBag.WorkflowStatuses = await GetNDCWorkflowStatusesAsync();
            return View(list);
        }

        public async Task<IActionResult> Create(string? customerId = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var workflowStatuses = await GetNDCWorkflowStatusesAsync();
            var ndcTypes = await GetNDCTypesAsync();
            var defaultStatus = workflowStatuses.Length > 0 ? workflowStatuses[0] : "Initiated";

            ViewBag.WorkflowStatuses = workflowStatuses;
            ViewBag.NDCTypes = ndcTypes;
            ViewBag.NDCExpiryDays = await GetNDCExpiryDaysAsync();
            ViewBag.NDCStartNormalDays = await GetNDCStartNormalDaysAsync();
            ViewBag.NDCStartUrgentDays = await GetNDCStartUrgentDaysAsync();

            var model = new NDC
            {
                WorkFlowStatus = defaultStatus,
                CreatedAt = DateTime.Now
            };
            if (!string.IsNullOrWhiteSpace(customerId))
                model.CustomerID = customerId.Trim();
            return View(model);
        }

        /// <summary>AJAX: Customer info and due/paid summary. Transfer fee is loaded separately when NDC Type is selected (see GetNdcTransferFee).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCustomerForNDC(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return Json(new { success = false, message = "Customer ID is required." });

            var customer = await _context.Customers
                .Include(c => c.PaymentPlan)
                    .ThenInclude(pp => pp!.PaymentSchedules)
                .Include(c => c.PaymentPlan)
                    .ThenInclude(pp => pp!.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == customerId.Trim());

            if (customer == null)
                return Json(new { success = false, message = "Customer not found." });

            var planId = customer.PlanID;
            if (string.IsNullOrEmpty(planId))
                return Json(new { success = false, message = "Customer has no payment plan." });

            var asOfDate = DateTime.Today;

            // Total due: sum of PaymentSchedule.Amount for schedules in customer's plan with DueDate <= asOfDate
            var totalDueAmount = await _context.PaymentSchedules
                .Where(ps => ps.PlanID == planId && ps.DueDate <= asOfDate)
                .SumAsync(ps => ps.Amount);

            // Total paid: sum of Payments for this customer
            var totalPaidAmount = await _context.Payments
                .Where(p => p.CustomerID == customerId)
                .SumAsync(p => p.Amount);

            var remainingDues = Math.Max(totalDueAmount - totalPaidAmount, 0m);
            var allPaymentClear = remainingDues <= 0m;

            var today = DateTime.Today;
            var hasActiveNDC = await _context.NDCs
                .AnyAsync(n => n.CustomerID == customerId.Trim() && n.NDCExpiryDate.HasValue && n.NDCExpiryDate.Value >= today);

            var message = !allPaymentClear
                ? $"Customer has remaining dues of {remainingDues:N0}."
                : hasActiveNDC
                    ? "Customer has an active NDC, but creation is allowed. Please verify dates and workflow."
                    : "No remaining dues.";

            var projectId = customer.ProjectID ?? customer.PaymentPlan?.ProjectID;
            var subProject = customer.SubProject;
            var propertySize = ParsePropertySizeValue(customer.RegisteredSize);
            var addTransferFeeUrl = Url.Action("Create", "TransferFee", new { projectId, subProject });

            return Json(new
            {
                success = true,
                customerID = customer.CustomerID,
                fullName = customer.FullName,
                fatherName = customer.FatherName,
                cnic = customer.CNIC,
                phone = customer.Phone,
                email = customer.Email,
                address = customer.MailingAddress ?? customer.PermanentAddress,
                planName = customer.PaymentPlan?.PlanName,
                projectName = customer.PaymentPlan?.Project?.ProjectName,
                projectId,
                subProject,
                totalDueAmount,
                totalPaidAmount,
                remainingDues,
                allPaymentClear,
                hasActiveNDC,
                message,
                propertySize,
                addTransferFeeUrl,
                feePendingNdcType = true
            });
        }

        /// <summary>AJAX: Looks up Transfer Fee row where TransferType matches the selected NDC Type (and project / sub-project from customer).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetNdcTransferFee(string customerId, string ndcType)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return Json(new { success = false, message = "Customer ID is required." });
            if (string.IsNullOrWhiteSpace(ndcType))
                return Json(new { success = false, message = "Select NDC Type to load the fee from the Transfer Fee module (Transfer Type must match NDC Type)." });

            var customer = await _context.Customers
                .Include(c => c.PaymentPlan)
                    .ThenInclude(pp => pp!.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == customerId.Trim());

            if (customer == null)
                return Json(new { success = false, message = "Customer not found. Search the customer first." });

            var projectId = customer.ProjectID ?? customer.PaymentPlan?.ProjectID;
            var subProject = customer.SubProject;
            var projectName = customer.PaymentPlan?.Project?.ProjectName;
            var transferType = ndcType.Trim();
            var transferPriority = ResolveTransferPriority(ndcType);
            var transferFee = await FindTransferFeeAsync(projectId, subProject, transferType, transferPriority);
            var propertySize = ParsePropertySizeValue(customer.RegisteredSize);
            var addTransferFeeUrl = Url.Action("Create", "TransferFee", new { projectId, subProject });

            if (transferFee == null)
            {
                return Json(new
                {
                    success = true,
                    transferFeeExists = false,
                    transferType,
                    transferPriority,
                    propertySize,
                    amountPerUnit = 0m,
                    transferFeeAmount = 0m,
                    addTransferFeeUrl,
                    message = $"No Transfer Fee row for Transfer Type '{transferType}' (NDC Type), project '{projectName ?? projectId ?? "N/A"}', sub-project '{subProject ?? "N/A"}', and priority '{transferPriority}'. Add a matching row in Transfer Fee."
                });
            }

            var amountPerUnit = transferFee.AmountPerUnit;
            var transferFeeAmount = Math.Round(amountPerUnit * propertySize, 2, MidpointRounding.AwayFromZero);

            return Json(new
            {
                success = true,
                transferFeeExists = true,
                transferType,
                transferPriority,
                propertySize,
                amountPerUnit,
                transferFeeAmount,
                addTransferFeeUrl,
                message = (string?)null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NDC model, IFormFile? ndcAttachment = null, string? ndcAttachmentTitle = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(model.CustomerID))
            {
                await SetNDCConfigViewBagAsync();
                ModelState.AddModelError("CustomerID", "Customer ID is required.");
                return View(model);
            }

            var customer = await _context.Customers
                .Include(c => c.PaymentPlan)
                .FirstOrDefaultAsync(c => c.CustomerID == model.CustomerID.Trim());

            if (customer == null)
            {
                await SetNDCConfigViewBagAsync();
                ModelState.AddModelError("CustomerID", "Customer not found.");
                return View(model);
            }

            var planId = customer.PlanID;
            if (string.IsNullOrEmpty(planId))
            {
                await SetNDCConfigViewBagAsync();
                ModelState.AddModelError("CustomerID", "Customer has no payment plan.");
                return View(model);
            }

            var projectId = customer.ProjectID ?? customer.PaymentPlan?.ProjectID;
            var subProject = customer.SubProject;
            var transferPriority = ResolveTransferPriority(model.NDCType);
            var transferType = string.IsNullOrWhiteSpace(model.NDCType) ? null : model.NDCType.Trim();
            var transferFee = await FindTransferFeeAsync(projectId, subProject, transferType, transferPriority);
            if (transferFee == null)
            {
                await SetNDCConfigViewBagAsync();
                ModelState.AddModelError("", $"Transfer fee does not exist for Project '{projectId ?? "N/A"}' and SubProject '{subProject ?? "N/A"}'.");
                return View(model);
            }

            var propertySize = ParsePropertySizeValue(customer.RegisteredSize);
            if (propertySize <= 0m)
            {
                await SetNDCConfigViewBagAsync();
                ModelState.AddModelError("", "Customer Registered Size does not contain a numeric value. Please correct size before creating NDC.");
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            var asOfDate = model.CreatedAt;

            var totalDueAmount = await _context.PaymentSchedules
                .Where(ps => ps.PlanID == planId && ps.DueDate <= asOfDate)
                .SumAsync(ps => ps.Amount);

            var totalPaidAmount = await _context.Payments
                .Where(p => p.CustomerID == model.CustomerID)
                .SumAsync(p => p.Amount);

            var remainingDues = Math.Max(totalDueAmount - totalPaidAmount, 0m);
            var allPaymentClear = remainingDues <= 0m;

            var expiryDays = await GetNDCExpiryDaysAsync();
            var startNormalDays = await GetNDCStartNormalDaysAsync();
            var startUrgentDays = await GetNDCStartUrgentDaysAsync();
            var isUrgentType = !string.IsNullOrWhiteSpace(model.NDCType) && model.NDCType.Contains("Urgent", StringComparison.OrdinalIgnoreCase);
            var issuedDateOffsetDays = isUrgentType ? startUrgentDays : startNormalDays;
            var workflowStatuses = await GetNDCWorkflowStatusesAsync();
            var defaultStatus = workflowStatuses.Length > 0 ? workflowStatuses[0] : "Initiated";

            model.NDCID = GenerateID();
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value;
            model.IssuedDate = model.CreatedAt.AddDays(issuedDateOffsetDays);
            model.NDCExpiryDate = model.CreatedAt.Date.AddDays(expiryDays);
            model.TotalDueAmount = totalDueAmount;
            model.TotalDueInstallments = totalPaidAmount;
            model.RemainingDues = remainingDues;
            model.AllPaymentClear = allPaymentClear;
            model.AmountPerUnit = transferFee.AmountPerUnit;
            model.PropertySize = propertySize;
            model.TransferFeeAmount = Math.Round(transferFee.AmountPerUnit * propertySize, 2, MidpointRounding.AwayFromZero);
            model.WorkFlowStatus ??= defaultStatus;

            _context.NDCs.Add(model);
            await _context.SaveChangesAsync();

            if (ndcAttachment != null && ndcAttachment.Length > 0)
            {
                var uploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var attachmentError = await SaveNdcAttachmentAsync(model.NDCID, ndcAttachment, ndcAttachmentTitle, uploadedBy);
                if (attachmentError != null)
                {
                    TempData["Error"] = attachmentError;
                    return RedirectToAction(nameof(Edit), new { id = model.NDCID });
                }
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Create NDC", "NDC", model.NDCID);
            }

            TempData["Success"] = "NDC created successfully. Issued Date: " + model.IssuedDate.ToString("MMM dd, yyyy") + ", Expiry: " + model.NDCExpiryDate?.ToString("MMM dd, yyyy") + ", Remaining Dues: " + remainingDues.ToString("N0") + ".";
            return RedirectToAction(nameof(Details), new { id = model.NDCID });
        }

        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var ndc = await _context.NDCs
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .FirstOrDefaultAsync(n => n.NDCID == id);

            if (ndc == null)
                return NotFound();

            ViewBag.NdcAttachments = await _context.Attachments
                .AsNoTracking()
                .Where(a => a.RefType == "NDC" && a.RefID == id)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "View NDC Details", "NDC", ndc.NDCID);
            }

            return View(ndc);
        }

        /// <summary>Print-friendly NDC view; opens in new tab. Same theme as Account Statement.</summary>
        public async Task<IActionResult> Print(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var ndc = await _context.NDCs
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.JointOwners)
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.PaymentSchedules)
                            .ThenInclude(ps => ps.Payments.Where(p => p.AuditStatus == "Approved"))
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.NDCID == id);

            if (ndc == null)
                return NotFound();

            var schedules = ndc.Customer?.PaymentPlan?.PaymentSchedules ?? new List<PaymentSchedule>();
            var surchargeBySchedule = _surchargeService.ComputeBySchedule(
                schedules,
                ndc.CustomerID,
                DateTime.Now.Date);
            ViewBag.TotalDueSurcharge = surchargeBySchedule.Values.Sum(x => x.Surcharge);

            ViewBag.NdcAttachments = await _context.Attachments
                .AsNoTracking()
                .Where(a => a.RefType == "NDC" && a.RefID == id)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Print NDC", "NDC", ndc.NDCID);
            }

            return View(ndc);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var ndc = await _context.NDCs
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .FirstOrDefaultAsync(n => n.NDCID == id);

            if (ndc == null)
                return NotFound();

            if (IsApprovedStatus(ndc.WorkFlowStatus))
            {
                TempData["Error"] = "Approved NDC cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.NdcAttachments = await _context.Attachments
                .AsNoTracking()
                .Where(a => a.RefType == "NDC" && a.RefID == id)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
            await SetNDCConfigViewBagAsync();
            return View(ndc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, NDC model, IFormFile? ndcAttachment = null, string? ndcAttachmentTitle = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id) || id != model.NDCID)
                return NotFound();

            var existing = await _context.NDCs
                .Include(n => n.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .FirstOrDefaultAsync(n => n.NDCID == id);

            if (existing == null)
                return NotFound();

            if (IsApprovedStatus(existing.WorkFlowStatus))
            {
                TempData["Error"] = "Approved NDC cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            existing.NDCType = model.NDCType;
            existing.Title = model.Title;
            existing.WorkFlowStatus = model.WorkFlowStatus;
            existing.Comments = model.Comments;
            existing.Remarks = model.Remarks;

            await _context.SaveChangesAsync();

            if (ndcAttachment != null && ndcAttachment.Length > 0)
            {
                var uploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var attachmentError = await SaveNdcAttachmentAsync(existing.NDCID, ndcAttachment, ndcAttachmentTitle, uploadedBy);
                if (attachmentError != null)
                {
                    TempData["Error"] = attachmentError;
                    return RedirectToAction(nameof(Edit), new { id = existing.NDCID });
                }
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Edit NDC", "NDC", existing.NDCID);
            }

            TempData["Success"] = "NDC updated successfully.";
            return RedirectToAction(nameof(Details), new { id = existing.NDCID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var ndc = await _context.NDCs.FirstOrDefaultAsync(n => n.NDCID == id);
            if (ndc == null)
            {
                TempData["Error"] = "NDC record not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.NDCs.Remove(ndc);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Delete NDC", "NDC", id);
            }

            TempData["Success"] = "NDC deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private static bool IsApprovedStatus(string? status)
        {
            return string.Equals(status?.Trim(), "Approved", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveTransferPriority(string? ndcType)
        {
            return !string.IsNullOrWhiteSpace(ndcType) &&
                   ndcType.Contains("Urgent", StringComparison.OrdinalIgnoreCase)
                ? "Urgent"
                : "Normal";
        }

        private static decimal ParsePropertySizeValue(string? registeredSize)
        {
            if (string.IsNullOrWhiteSpace(registeredSize))
                return 0m;

            var match = Regex.Match(registeredSize, @"\d+(\.\d+)?");
            if (!match.Success)
                return 0m;

            return decimal.TryParse(match.Value, out var parsed) ? parsed : 0m;
        }

        private async Task<TransferFee?> FindTransferFeeAsync(string? projectId, string? subProject, string? transferType, string? transferPriority)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return null;

            var normalizedSubProject = (subProject ?? string.Empty).Trim();
            var normalizedType = (transferType ?? string.Empty).Trim();
            var normalizedPriority = (transferPriority ?? string.Empty).Trim();

            var candidates = await _context.TransferFees
                .AsNoTracking()
                .Where(t => t.ProjectID == projectId)
                .OrderByDescending(t => t.CreatedOn)
                .ToListAsync();

            var byProjectAndSub = candidates
                .Where(t => string.Equals((t.SubProject ?? string.Empty).Trim(), normalizedSubProject, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!byProjectAndSub.Any())
                return null;

            var exact = byProjectAndSub.FirstOrDefault(t =>
                string.Equals((t.TransferType ?? string.Empty).Trim(), normalizedType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((t.TransferPriority ?? string.Empty).Trim(), normalizedPriority, StringComparison.OrdinalIgnoreCase));

            if (exact != null)
                return exact;

            // Legacy-safe fallback: match project + subproject + transfer type.
            var byType = byProjectAndSub.FirstOrDefault(t =>
                string.Equals((t.TransferType ?? string.Empty).Trim(), normalizedType, StringComparison.OrdinalIgnoreCase));
            if (byType != null)
                return byType;

            // Final safe fallback: match project + subproject + transfer priority.
            var byPriority = byProjectAndSub.FirstOrDefault(t =>
                string.Equals((t.TransferPriority ?? string.Empty).Trim(), normalizedPriority, StringComparison.OrdinalIgnoreCase));
            return byPriority;
        }

        private static string SanitizePathSegment(string? segment)
        {
            var s = (segment ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');

            s = s.Replace(Path.DirectorySeparatorChar, '_')
                 .Replace(Path.AltDirectorySeparatorChar, '_')
                 .Trim()
                 .TrimEnd('.');
            return s;
        }

        private async Task<string?> SaveNdcAttachmentAsync(string ndcId, IFormFile file, string? description, string? uploadedByRaw)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedNdcAttachmentExtensions.Contains(ext))
                return "Only image files (JPG, PNG, GIF, BMP) and PDF are allowed.";
            if (file.Length > MaxNdcAttachmentSize)
                return "Attachment file size exceeds 8MB limit.";

            var safeId = SanitizePathSegment(ndcId);
            if (string.IsNullOrWhiteSpace(safeId))
                return "Invalid NDC ID for file storage.";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ndc", safeId);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var relativePath = $"/uploads/ndc/{safeId}/{uniqueFileName}";

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var uploadedBy = string.IsNullOrEmpty(uploadedByRaw)
                ? null
                : (uploadedByRaw.Length <= 10 ? uploadedByRaw : uploadedByRaw[..10]);

            var attachment = new Attachment
            {
                AttachmentID = GenerateID(),
                RefType = "NDC",
                RefID = ndcId,
                AttachmentType = "Proof",
                FileName = file.FileName,
                FilePath = relativePath,
                FileSize = file.Length,
                FileType = file.ContentType,
                Description = string.IsNullOrWhiteSpace(description) ? "NDC attachment" : description.Trim(),
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();
            return null;
        }

        private async Task LogActivity(string userId, string action, string refType, string refId)
        {
            try
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = action,
                    RefType = refType,
                    RefID = refId,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            catch { /* ignore */ }
        }

        private static string GenerateID()
        {
            return Guid.NewGuid().ToString("N")[..10].ToUpper();
        }
    }
}
