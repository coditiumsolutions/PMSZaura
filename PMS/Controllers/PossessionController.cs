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
    public class PossessionController : Controller
    {
        private const string ModuleKey = "Possession";
        private const string RefType = "Possession";

        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        private static readonly string[] AllowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private const long MaxAttachmentSize = 8 * 1024 * 1024;

        public PossessionController(PMSDbContext context, IModulePermissionService modulePermission)
        {
            _context = context;
            _modulePermission = modulePermission;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");
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

        private async Task<List<string>> GetWorkflowStatusesAsync()
        {
            var config = await _context.Configurations.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigKey == "possessionworkflow");
            return config?.ConfigValue != null
                ? config.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string> { "Initiated", "Operations Desk", "Approved", "Declined" };
        }

        private async Task SetWorkflowStatusesViewBagAsync()
        {
            var statuses = await GetWorkflowStatusesAsync();
            ViewBag.WorkflowStatuses = statuses;
            ViewBag.InitiatedStatus = ResolveWorkflowStatus(statuses, "Initiated");
            ViewBag.OperationsDeskStatus = ResolveWorkflowStatus(statuses, "Operations Desk");
            ViewBag.ApprovedStatus = ResolveWorkflowStatus(statuses, "Approved");
            ViewBag.DeclinedStatus = ResolveWorkflowStatus(statuses, "Declined");
        }

        private static string ResolveWorkflowStatus(IEnumerable<string> statuses, string fallback) =>
            statuses.FirstOrDefault(s => string.Equals(s, fallback, StringComparison.OrdinalIgnoreCase)) ?? fallback;

        private static int GetWorkflowIndex(List<string> statuses, string? currentStatus)
        {
            if (statuses.Count == 0 || string.IsNullOrWhiteSpace(currentStatus)) return -1;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (string.Equals(statuses[i], currentStatus, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void AddActivityLog(string? userId, string action, string refId)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID = userId,
                Action = action,
                RefType = RefType,
                RefID = refId,
                CreatedAt = DateTime.Now
            });
        }

        private async Task<string> GeneratePossessionIdAsync()
        {
            var prefix = $"POS-{DateTime.UtcNow:yyyyMMdd}-";
            var last = await _context.Possessions
                .Where(p => p.PossessionID.StartsWith(prefix))
                .OrderByDescending(p => p.PossessionID)
                .Select(p => p.PossessionID)
                .FirstOrDefaultAsync();
            var seq = 1;
            if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length)
            {
                var tail = last[prefix.Length..];
                if (int.TryParse(tail, out var n)) seq = n + 1;
            }
            return prefix + seq.ToString("D4");
        }

        public async Task<IActionResult> Index(string workflowFilter = "", string customerFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var query = _context.Possessions
                .Include(p => p.Customer)
                .Include(p => p.Property)
                .Include(p => p.CreatedByUser)
                .AsNoTracking()
                .AsQueryable();

            await SetWorkflowStatusesViewBagAsync();
            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string>();
            var approvedStatus = (string?)ViewBag.ApprovedStatus ?? "Approved";
            var declinedStatus = (string?)ViewBag.DeclinedStatus ?? "Declined";

            if (!string.IsNullOrWhiteSpace(workflowFilter))
            {
                if (string.Equals(workflowFilter, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p =>
                        p.WorkFlowStatus == null
                        || (p.WorkFlowStatus != approvedStatus && p.WorkFlowStatus != declinedStatus));
                }
                else
                {
                    query = query.Where(p => p.WorkFlowStatus == workflowFilter);
                }
            }

            if (!string.IsNullOrWhiteSpace(customerFilter))
                query = query.Where(p => p.CustomerID == customerFilter.Trim());

            var list = await query.OrderByDescending(p => p.CreatedAt ?? p.PossessionDate).ToListAsync();

            ViewBag.WorkflowFilter = workflowFilter;
            ViewBag.CustomerFilter = customerFilter;
            ViewBag.PendingCount = await _context.Possessions.CountAsync(p =>
                p.WorkFlowStatus == null
                || (p.WorkFlowStatus != approvedStatus && p.WorkFlowStatus != declinedStatus));
            ViewBag.ApprovedCount = await _context.Possessions.CountAsync(p => p.WorkFlowStatus == approvedStatus);
            ViewBag.DeclinedCount = await _context.Possessions.CountAsync(p => p.WorkFlowStatus == declinedStatus);

            return View(list);
        }

        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();
            var initiated = (string?)ViewBag.InitiatedStatus ?? "Initiated";
            return View(new Possession { WorkFlowStatus = initiated, PossessionDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Possession model, IFormFile? attachment = null, string? attachmentDescription = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var initiated = (string?)ViewBag.InitiatedStatus ?? "Initiated";

            if (string.IsNullOrWhiteSpace(model.PropertyID) || !await _context.Properties.AnyAsync(p => p.PropertyID == model.PropertyID.Trim()))
                ModelState.AddModelError(nameof(model.PropertyID), "Valid Property ID is required.");
            if (string.IsNullOrWhiteSpace(model.CustomerID) || !await _context.Customers.AnyAsync(c => c.CustomerID == model.CustomerID.Trim()))
                ModelState.AddModelError(nameof(model.CustomerID), "Valid Customer ID is required.");

            if (!ModelState.IsValid)
                return View(model);

            model.PropertyID = model.PropertyID?.Trim();
            model.CustomerID = model.CustomerID?.Trim();
            model.PossessionID = await GeneratePossessionIdAsync();
            model.WorkFlowStatus = initiated;
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = userId;
            model.ModifiedAt = null;
            model.ModifiedBy = null;
            if (model.PossessionDueCharges < 0) model.PossessionDueCharges = 0;
            if (model.PossessionPaidCharges < 0) model.PossessionPaidCharges = 0;

            _context.Possessions.Add(model);

            if (attachment != null && attachment.Length > 0)
            {
                var err = await SaveAttachmentAsync(model.PossessionID, attachment, attachmentDescription, userId);
                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(string.Empty, err);
                    _context.Possessions.Remove(model);
                    return View(model);
                }
            }

            AddActivityLog(userId, $"Possession {model.PossessionID} created (workflow: {initiated}).", model.PossessionID);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Possession {model.PossessionID} created.";
            return RedirectToAction(nameof(Details), new { id = model.PossessionID });
        }

        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var row = await _context.Possessions
                .Include(p => p.Customer)
                .Include(p => p.Property)
                .Include(p => p.CreatedByUser)
                .Include(p => p.ModifiedByUser)
                .Include(p => p.ApprovedByUser)
                .Include(p => p.DeclinedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PossessionID == id);
            if (row == null) return NotFound();
            return View(row);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var row = await _context.Possessions
                .Include(p => p.Customer)
                .Include(p => p.Property)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PossessionID == id);
            if (row == null) return NotFound();

            var approved = (string?)ViewBag.ApprovedStatus ?? "Approved";
            var declined = (string?)ViewBag.DeclinedStatus ?? "Declined";
            if (string.Equals(row.WorkFlowStatus, approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.WorkFlowStatus, declined, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Approved or declined possession requests cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string>();
            var initiated = (string?)ViewBag.InitiatedStatus ?? "Initiated";
            var ops = (string?)ViewBag.OperationsDeskStatus ?? "Operations Desk";
            var idxInit = GetWorkflowIndex(workflowStatuses, initiated);
            var idxOps = GetWorkflowIndex(workflowStatuses, ops);
            var currentIdx = GetWorkflowIndex(workflowStatuses, row.WorkFlowStatus);
            var atInitiated = currentIdx == idxInit && idxInit >= 0;
            var atOps = currentIdx == idxOps && idxOps >= 0;
            ViewBag.CanMoveForward = atInitiated && idxOps >= 0;
            ViewBag.CanMoveBackward = atOps && idxInit >= 0;
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Possession model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var existing = await _context.Possessions.FirstOrDefaultAsync(p => p.PossessionID == id);
            if (existing == null) return NotFound();

            var approved = (string?)ViewBag.ApprovedStatus ?? "Approved";
            var declined = (string?)ViewBag.DeclinedStatus ?? "Declined";
            if (string.Equals(existing.WorkFlowStatus, approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(existing.WorkFlowStatus, declined, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Approved or declined possession requests cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(model.PropertyID) || !await _context.Properties.AnyAsync(p => p.PropertyID == model.PropertyID.Trim()))
                ModelState.AddModelError(nameof(model.PropertyID), "Valid Property ID is required.");
            if (string.IsNullOrWhiteSpace(model.CustomerID) || !await _context.Customers.AnyAsync(c => c.CustomerID == model.CustomerID.Trim()))
                ModelState.AddModelError(nameof(model.CustomerID), "Valid Customer ID is required.");

            if (!ModelState.IsValid)
            {
                model.PossessionID = id;
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            existing.PropertyID = model.PropertyID?.Trim();
            existing.CustomerID = model.CustomerID?.Trim();
            existing.PossessionDate = model.PossessionDate;
            existing.Comments = model.Comments;
            existing.Remarks = model.Remarks;
            existing.PossessionDueCharges = model.PossessionDueCharges < 0 ? 0 : model.PossessionDueCharges;
            existing.PossessionPaidCharges = model.PossessionPaidCharges < 0 ? 0 : model.PossessionPaidCharges;
            existing.BankName = model.BankName;
            existing.PaidDate = model.PaidDate;
            existing.InstrumentNo = model.InstrumentNo;
            existing.PaymentMethod = model.PaymentMethod;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = userId;

            AddActivityLog(userId, $"Possession {id} updated.", id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Possession updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveStatus(string id, string direction)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var row = await _context.Possessions.FirstOrDefaultAsync(p => p.PossessionID == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }

            var workflowStatuses = await GetWorkflowStatusesAsync();
            var initiated = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            var ops = ResolveWorkflowStatus(workflowStatuses, "Operations Desk");
            var approved = ResolveWorkflowStatus(workflowStatuses, "Approved");
            var declined = ResolveWorkflowStatus(workflowStatuses, "Declined");

            if (string.Equals(row.WorkFlowStatus, approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.WorkFlowStatus, declined, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Workflow cannot be moved from a terminal status.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var idxInit = GetWorkflowIndex(workflowStatuses, initiated);
            var idxOps = GetWorkflowIndex(workflowStatuses, ops);
            var cur = GetWorkflowIndex(workflowStatuses, row.WorkFlowStatus);
            if (idxInit < 0 || idxOps < 0)
            {
                TempData["ErrorMessage"] = "Configured possessionworkflow must include Initiated and Operations Desk.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var forward = string.Equals(direction, "forward", StringComparison.OrdinalIgnoreCase);
            var backward = string.Equals(direction, "backward", StringComparison.OrdinalIgnoreCase);
            if (!forward && !backward)
            {
                TempData["ErrorMessage"] = "Invalid direction.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (forward && cur == idxInit)
                row.WorkFlowStatus = ops;
            else if (backward && cur == idxOps)
                row.WorkFlowStatus = initiated;
            else
            {
                TempData["ErrorMessage"] = "That workflow move is not allowed from the current status.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            row.ModifiedAt = DateTime.Now;
            row.ModifiedBy = userId;
            AddActivityLog(userId, $"Possession {id} workflow moved to {row.WorkFlowStatus}.", id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Status is now {row.WorkFlowStatus}.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string? notes)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var row = await _context.Possessions.FirstOrDefaultAsync(p => p.PossessionID == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }

            var workflowStatuses = await GetWorkflowStatusesAsync();
            var ops = ResolveWorkflowStatus(workflowStatuses, "Operations Desk");
            var approved = ResolveWorkflowStatus(workflowStatuses, "Approved");
            if (!string.Equals(row.WorkFlowStatus, ops, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Approve is only allowed from {ops}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            row.WorkFlowStatus = approved;
            row.ApprovedBy = userId;
            row.ApprovedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(notes))
                row.Remarks = string.IsNullOrWhiteSpace(row.Remarks) ? notes : row.Remarks + Environment.NewLine + "[Approve] " + notes;
            row.ModifiedAt = DateTime.Now;
            row.ModifiedBy = userId;

            AddActivityLog(userId, $"Possession {id} approved.", id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Possession approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(string id, string? notes)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var row = await _context.Possessions.FirstOrDefaultAsync(p => p.PossessionID == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }

            var workflowStatuses = await GetWorkflowStatusesAsync();
            var ops = ResolveWorkflowStatus(workflowStatuses, "Operations Desk");
            var declined = ResolveWorkflowStatus(workflowStatuses, "Declined");
            if (!string.Equals(row.WorkFlowStatus, ops, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Decline is only allowed from {ops}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            row.WorkFlowStatus = declined;
            row.DeclinedBy = userId;
            row.DeclinedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(notes))
                row.Remarks = string.IsNullOrWhiteSpace(row.Remarks) ? notes : row.Remarks + Environment.NewLine + "[Decline] " + notes;
            row.ModifiedAt = DateTime.Now;
            row.ModifiedBy = userId;

            AddActivityLog(userId, $"Possession {id} declined.", id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Possession declined.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();

            var row = await _context.Possessions.FirstOrDefaultAsync(p => p.PossessionID == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }

            var approved = (string?)ViewBag.ApprovedStatus ?? "Approved";
            if (string.Equals(row.WorkFlowStatus, approved, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Approved possession requests cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var atts = await _context.Attachments.Where(a => a.RefType == RefType && a.RefID == id).ToListAsync();
            foreach (var a in atts)
            {
                if (!string.IsNullOrEmpty(a.FilePath))
                {
                    var trimmed = a.FilePath.TrimStart('~').TrimStart('/');
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trimmed.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
            }
            _context.Attachments.RemoveRange(atts);
            _context.Possessions.Remove(row);
            AddActivityLog(userId, $"Possession {id} deleted.", id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Possession deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(string possessionId, IFormFile file, string description = "")
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return Json(new { success = false, message = "Access denied." });
            try
            {
                var pid = (possessionId ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(pid))
                    return Json(new { success = false, message = "Possession ID is required." });
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Please select a file." });

                var row = await _context.Possessions.FindAsync(pid);
                if (row == null)
                    return Json(new { success = false, message = "Record not found." });
                var wfList = await GetWorkflowStatusesAsync();
                var approved = ResolveWorkflowStatus(wfList, "Approved");
                if (string.Equals(row.WorkFlowStatus, approved, StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Cannot attach files to an approved possession request." });

                var uploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var err = await SaveAttachmentAsync(pid, file, description, uploadedBy);
                if (!string.IsNullOrEmpty(err))
                    return Json(new { success = false, message = err });
                AddActivityLog(uploadedBy, $"Possession {pid} attachment uploaded.", pid);
                await _context.SaveChangesAsync();

                var attachment = await _context.Attachments
                    .Where(a => a.RefType == RefType && a.RefID == pid)
                    .OrderByDescending(a => a.UploadedAt)
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    message = "Uploaded",
                    attachment = attachment == null ? null : new
                    {
                        attachmentID = attachment.AttachmentID,
                        fileName = attachment.FileName,
                        filePath = attachment.FilePath,
                        fileSize = attachment.FileSize,
                        fileType = attachment.FileType,
                        description = attachment.Description,
                        uploadedAt = attachment.UploadedAt.ToString("MMM dd, yyyy hh:mm tt")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttachments(string possessionId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return Json(new { success = false, message = "Access denied." });
            var pid = (possessionId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(pid))
                return Json(new { success = false, message = "Possession ID is required." });
            var list = await _context.Attachments.AsNoTracking()
                .Where(a => a.RefType == RefType && a.RefID == pid)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new
                {
                    attachmentID = a.AttachmentID,
                    fileName = a.FileName,
                    filePath = a.FilePath,
                    fileSize = a.FileSize,
                    fileType = a.FileType,
                    description = a.Description,
                    uploadedAt = a.UploadedAt.ToString("MMM dd, yyyy hh:mm tt")
                })
                .ToListAsync();
            return Json(new { success = true, attachments = list });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(string attachmentId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return Json(new { success = false, message = "Access denied." });
            if (string.IsNullOrEmpty(attachmentId))
                return Json(new { success = false, message = "Attachment ID required." });

            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a => a.AttachmentID == attachmentId && a.RefType == RefType);
            if (attachment == null)
                return Json(new { success = false, message = "Attachment not found." });

            var row = await _context.Possessions.FindAsync(attachment.RefID);
            var wfList = await GetWorkflowStatusesAsync();
            var approved = ResolveWorkflowStatus(wfList, "Approved");
            if (row != null && string.Equals(row.WorkFlowStatus, approved, StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Cannot delete attachments on an approved possession request." });

            if (!string.IsNullOrEmpty(attachment.FilePath))
            {
                var trimmed = attachment.FilePath.TrimStart('~').TrimStart('/');
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trimmed.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            _context.Attachments.Remove(attachment);
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            AddActivityLog(uid, $"Possession {attachment.RefID} attachment {attachmentId} deleted.", attachment.RefID ?? "");
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Deleted." });
        }

        private async Task<string?> SaveAttachmentAsync(string possessionId, IFormFile file, string? description, string? uploadedByRaw)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedAttachmentExtensions.Contains(ext))
                return "Only JPG, PNG, GIF, BMP, PDF are allowed.";
            if (file.Length > MaxAttachmentSize)
                return "File exceeds 8MB.";

            var safeId = SanitizePathSegment(possessionId);
            if (string.IsNullOrEmpty(safeId))
                return "Invalid Possession ID for storage.";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "possessions", safeId);
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var relativePath = $"/uploads/possessions/{safeId}/{uniqueFileName}";
            var attachmentId = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var uploadedBy = string.IsNullOrEmpty(uploadedByRaw) ? null
                : (uploadedByRaw.Length <= 10 ? uploadedByRaw : uploadedByRaw[..10]);

            _context.Attachments.Add(new Attachment
            {
                AttachmentID = attachmentId,
                RefType = RefType,
                RefID = possessionId,
                AttachmentType = RefType,
                FileName = file.FileName,
                FilePath = relativePath,
                FileSize = file.Length,
                FileType = file.ContentType,
                Description = description,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            });
            return null;
        }

        private static string SanitizePathSegment(string? segment)
        {
            var s = (segment ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s)) return string.Empty;
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            s = s.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_').Trim().TrimEnd('.');
            return s;
        }
    }
}
