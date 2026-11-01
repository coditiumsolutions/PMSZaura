using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;

namespace Salzee.Controllers
{
    [Authorize]
    public class DuplicateFileTransferController : Controller
    {
        private const string ModuleKey = "DuplicateFileTransfer";
        private const string RefType = "DuplicateFileTransfer";
        private const string Initiated = "Initiated";
        private const string Approved = "Approved";
        private const string Declined = "Declined";

        private static readonly string[] AllowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private const long MaxAttachmentSize = 8 * 1024 * 1024;

        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public DuplicateFileTransferController(PMSDbContext context, IModulePermissionService modulePermission)
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

        private static string NormalizeId(string? id) => (id ?? string.Empty).Trim();

        private async Task<string?> ResolveCustomerIdAsync(string normalizedCustomerId)
        {
            if (string.IsNullOrEmpty(normalizedCustomerId)) return null;
            var exact = await _context.Customers.AsNoTracking()
                .Where(c => c.CustomerID == normalizedCustomerId)
                .Select(c => c.CustomerID)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(exact)) return exact;
            return await _context.Customers.AsNoTracking()
                .Where(c => c.CustomerID != null && c.CustomerID.Trim() == normalizedCustomerId)
                .Select(c => c.CustomerID)
                .FirstOrDefaultAsync();
        }

        private async Task<decimal?> LookupFeeDueAsync(string projectId, string? subProject)
        {
            var pid = NormalizeId(projectId);
            var sub = NormalizeId(subProject ?? string.Empty);
            var type = DuplicateFileTransfer.TransferFeeTypeName;
            var rows = await _context.TransferFees.AsNoTracking()
                .Where(t => t.ProjectID == pid
                    && (t.SubProject ?? "").Trim() == sub
                    && (t.TransferType ?? "").Trim() == type)
                .OrderBy(t => t.TransferPriority == "Urgent" ? 1 : 0)
                .ThenByDescending(t => t.CreatedOn)
                .Select(t => (decimal?)t.AmountPerUnit)
                .ToListAsync();
            return rows.FirstOrDefault();
        }

        private async Task<string> GenerateIdAsync()
        {
            var last = await _context.DuplicateFileTransfers
                .OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            var next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length >= 4)
            {
                var num = last[3..];
                if (int.TryParse(num, out var n)) next = n + 1;
            }
            return "DFT" + next.ToString("D7");
        }

        private static string TruncateUserId(string? userId) =>
            string.IsNullOrEmpty(userId) ? string.Empty : (userId.Length <= 10 ? userId : userId[..10]);

        private static string SanitizePathSegment(string? segment)
        {
            var s = NormalizeId(segment);
            if (string.IsNullOrEmpty(s)) return string.Empty;
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim().TrimEnd('.');
        }

        public async Task<IActionResult> Index(string workflowFilter = "", string customerFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var query = _context.DuplicateFileTransfers
                .Include(d => d.Customer)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(workflowFilter))
                query = query.Where(d => d.Status == workflowFilter);
            if (!string.IsNullOrWhiteSpace(customerFilter))
            {
                var cf = customerFilter.Trim();
                query = query.Where(d => d.CustomerID == cf || (d.CustomerID != null && d.CustomerID.Trim() == cf));
            }

            var list = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            ViewBag.WorkflowFilter = workflowFilter;
            ViewBag.CustomerFilter = customerFilter;
            ViewBag.InitiatedCount = await _context.DuplicateFileTransfers.CountAsync(d => d.Status == Initiated);
            ViewBag.ApprovedCount = await _context.DuplicateFileTransfers.CountAsync(d => d.Status == Approved);
            ViewBag.DeclinedCount = await _context.DuplicateFileTransfers.CountAsync(d => d.Status == Declined);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchCustomer(string customerId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return Json(new { success = false, message = "Access denied." });

            var normalized = NormalizeId(customerId);
            if (string.IsNullOrEmpty(normalized))
                return Json(new { success = false, message = "Enter a Customer ID." });

            var resolvedId = await ResolveCustomerIdAsync(normalized);
            if (string.IsNullOrEmpty(resolvedId))
                return Json(new { success = false, message = "Customer not found." });

            var customer = await _context.Customers.AsNoTracking()
                .Include(c => c.PaymentPlan).ThenInclude(p => p!.Project)
                .FirstOrDefaultAsync(c => c.CustomerID == resolvedId);

            if (customer == null)
                return Json(new { success = false, message = "Customer not found." });

            var projectId = (customer.ProjectID ?? customer.PaymentPlan?.ProjectID ?? string.Empty).Trim();
            var sub = (customer.SubProject ?? string.Empty).Trim();
            var feeDue = await LookupFeeDueAsync(projectId, sub);
            var addTransferFeeUrl = Url.Action("Create", "TransferFee", new { projectId, subProject = sub });

            return Json(new
            {
                success = true,
                customerID = customer.CustomerID,
                fullName = customer.FullName ?? "—",
                cnic = customer.CNIC ?? "—",
                project = customer.PaymentPlan?.Project?.ProjectName ?? "—",
                subProject = sub,
                projectId,
                feeDue,
                feeConfigured = feeDue.HasValue,
                addTransferFeeUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string customerId, string? comments, IFormFile? attachment = null, string? attachmentDescription = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var resolvedId = await ResolveCustomerIdAsync(NormalizeId(customerId));
            if (string.IsNullOrEmpty(resolvedId))
            {
                TempData["ErrorMessage"] = "Valid Customer ID is required.";
                return View();
            }

            var customer = await _context.Customers.AsNoTracking()
                .Include(c => c.PaymentPlan)
                .FirstOrDefaultAsync(c => c.CustomerID == resolvedId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return View();
            }

            var projectIdForFee = (customer.ProjectID ?? customer.PaymentPlan?.ProjectID ?? string.Empty).Trim();
            var feeDue = await LookupFeeDueAsync(projectIdForFee, customer.SubProject);
            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (attachment != null && attachment.Length > 0)
            {
                var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                if (!AllowedAttachmentExtensions.Contains(ext))
                {
                    TempData["ErrorMessage"] = "Attachment: only JPG, PNG, GIF, BMP, PDF allowed.";
                    return View();
                }
                if (attachment.Length > MaxAttachmentSize)
                {
                    TempData["ErrorMessage"] = "Attachment exceeds 8MB.";
                    return View();
                }
            }

            var newId = await GenerateIdAsync();
            var entity = new DuplicateFileTransfer
            {
                Id = newId,
                CustomerID = resolvedId,
                CustomerName = customer.FullName,
                CustomerCNIC = customer.CNIC,
                Status = Initiated,
                Comments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim(),
                FeeDue = feeDue,
                CreatedAt = DateTime.Now,
                CreatedBy = string.IsNullOrEmpty(userId) ? null : userId,
                ModifiedBy = string.IsNullOrEmpty(userId) ? null : userId
            };

            _context.DuplicateFileTransfers.Add(entity);

            if (attachment != null && attachment.Length > 0)
            {
                var err = await SaveAttachmentInternalAsync(newId, attachment, attachmentDescription, userId);
                if (!string.IsNullOrEmpty(err))
                {
                    _context.DuplicateFileTransfers.Remove(entity);
                    TempData["ErrorMessage"] = err;
                    return View();
                }
            }

            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Duplicate file transfer {entity.Id} initiated for {entity.CustomerID}.",
                    RefType = RefType,
                    RefID = entity.Id,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Request {entity.Id} created.";
            return RedirectToAction(nameof(Details), new { id = entity.Id });
        }

        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var row = await _context.DuplicateFileTransfers
                .Include(d => d.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (row == null) return NotFound();
            return View(row);
        }

        /// <summary>Printable letter for approved duplicate file transfer requests (customer / file info only; no buyer).</summary>
        [HttpGet]
        public async Task<IActionResult> DuplicateFileLetter(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            id = NormalizeId(id);
            if (string.IsNullOrEmpty(id)) return NotFound();

            var row = await _context.DuplicateFileTransfers
                .Include(d => d.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .Include(d => d.Customer)
                    .ThenInclude(c => c!.JointOwners)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (row == null) return NotFound();

            if (!string.Equals(row.Status, Approved, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Duplicate file letter is only available for approved requests.";
                return RedirectToAction(nameof(Details), new { id });
            }

            const string notAllotted = "Not allotted";
            var allotment = await _context.Allotments
                .AsNoTracking()
                .Include(a => a.Property)
                    .ThenInclude(p => p!.Project)
                .Where(a => a.CustomerID == row.CustomerID.Trim() && a.PropertyID != null)
                .OrderByDescending(a => a.AllotmentDate)
                .FirstOrDefaultAsync();

            var prop = allotment?.Property;
            if (prop == null)
            {
                ViewBag.PropertyProjectScheme = notAllotted;
                ViewBag.PropertyPhaseSector = notAllotted;
                ViewBag.PropertyPlotNo = notAllotted;
                ViewBag.PropertySize = notAllotted;
                ViewBag.LetterHeaderProject = row.Customer?.PaymentPlan?.Project?.ProjectName ?? notAllotted;
            }
            else
            {
                ViewBag.PropertyProjectScheme = prop.Project?.ProjectName ?? prop.ProjectID?.Trim() ?? "—";
                var phaseParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(prop.SubProject)) phaseParts.Add(prop.SubProject.Trim());
                if (!string.IsNullOrWhiteSpace(prop.Block)) phaseParts.Add(prop.Block.Trim());
                if (phaseParts.Count == 0 && !string.IsNullOrWhiteSpace(prop.Street)) phaseParts.Add(prop.Street.Trim());
                ViewBag.PropertyPhaseSector = phaseParts.Count > 0 ? string.Join(" / ", phaseParts) : "—";
                ViewBag.PropertyPlotNo = string.IsNullOrWhiteSpace(prop.PlotNo) ? "—" : prop.PlotNo.Trim();
                var sizeFromProp = prop.Size?.Trim();
                ViewBag.PropertySize = !string.IsNullOrEmpty(sizeFromProp)
                    ? sizeFromProp
                    : (row.Customer?.RegisteredSize ?? row.Customer?.PaymentPlan?.RegisteredSize ?? "—");
                ViewBag.LetterHeaderProject = prop.Project?.ProjectName ?? ViewBag.PropertyProjectScheme;
            }

            return View(row);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var row = await _context.DuplicateFileTransfers
                .Include(d => d.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (row == null) return NotFound();
            if (!string.Equals(row.Status, Initiated, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only initiated requests can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind(nameof(DuplicateFileTransfer.Comments), nameof(DuplicateFileTransfer.FeePaid), nameof(DuplicateFileTransfer.ChallanID), nameof(DuplicateFileTransfer.BankName), nameof(DuplicateFileTransfer.InstrumentNo), nameof(DuplicateFileTransfer.DepositDate), nameof(DuplicateFileTransfer.PaymentMethod))] DuplicateFileTransfer model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var existing = await _context.DuplicateFileTransfers.FirstOrDefaultAsync(d => d.Id == id);
            if (existing == null) return NotFound();
            if (!string.Equals(existing.Status, Initiated, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only initiated requests can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            existing.Comments = string.IsNullOrWhiteSpace(model.Comments) ? null : model.Comments.Trim();
            existing.FeePaid = model.FeePaid;
            existing.ChallanID = string.IsNullOrWhiteSpace(model.ChallanID) ? null : model.ChallanID.Trim();
            existing.BankName = string.IsNullOrWhiteSpace(model.BankName) ? null : model.BankName.Trim();
            existing.InstrumentNo = string.IsNullOrWhiteSpace(model.InstrumentNo) ? null : model.InstrumentNo.Trim();
            existing.DepositDate = model.DepositDate;
            existing.PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? null : model.PaymentMethod.Trim();
            existing.ModifiedBy = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (string.IsNullOrEmpty(existing.ModifiedBy)) existing.ModifiedBy = null;

            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Duplicate file transfer {id} updated.",
                    RefType = RefType,
                    RefID = id,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string? decisionNotes)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var row = await _context.DuplicateFileTransfers.FirstOrDefaultAsync(d => d.Id == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }
            if (!string.Equals(row.Status, Initiated, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Cannot approve: status is already {row.Status}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            row.Status = Approved;
            row.ModifiedBy = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (string.IsNullOrEmpty(row.ModifiedBy)) row.ModifiedBy = null;
            if (!string.IsNullOrWhiteSpace(decisionNotes))
            {
                var extra = decisionNotes.Trim();
                row.Comments = string.IsNullOrEmpty(row.Comments) ? extra : row.Comments + Environment.NewLine + "[Approve] " + extra;
            }

            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Duplicate file transfer {id} approved (no customer/payment changes).",
                    RefType = RefType,
                    RefID = id,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(string id, string? decisionNotes)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var row = await _context.DuplicateFileTransfers.FirstOrDefaultAsync(d => d.Id == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }
            if (!string.Equals(row.Status, Initiated, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Cannot decline: status is already {row.Status}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            row.Status = Declined;
            row.ModifiedBy = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (string.IsNullOrEmpty(row.ModifiedBy)) row.ModifiedBy = null;
            if (!string.IsNullOrWhiteSpace(decisionNotes))
            {
                var extra = decisionNotes.Trim();
                row.Comments = string.IsNullOrEmpty(row.Comments) ? extra : row.Comments + Environment.NewLine + "[Decline] " + extra;
            }

            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Duplicate file transfer {id} declined (no customer/payment changes).",
                    RefType = RefType,
                    RefID = id,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Declined.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            var row = await _context.DuplicateFileTransfers.FirstOrDefaultAsync(d => d.Id == id);
            if (row == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }
            if (!string.Equals(row.Status, Initiated, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only initiated requests can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var attachments = await _context.Attachments.Where(a => a.RefType == RefType && a.RefID == id).ToListAsync();
            foreach (var a in attachments)
            {
                if (!string.IsNullOrEmpty(a.FilePath))
                {
                    var trimmed = a.FilePath.TrimStart('~').TrimStart('/');
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trimmed.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
                _context.Attachments.Remove(a);
            }

            _context.DuplicateFileTransfers.Remove(row);
            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Duplicate file transfer {id} deleted.",
                    RefType = RefType,
                    RefID = id,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(string id, IFormFile file, string description = "")
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return Json(new { success = false, message = "Access denied." });

            var row = await _context.DuplicateFileTransfers.FirstOrDefaultAsync(d => d.Id == id);
            if (row == null) return Json(new { success = false, message = "Record not found." });
            if (!string.Equals(row.Status, Initiated, StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Attachments can only be added while status is Initiated." });

            var userId = TruncateUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var err = await SaveAttachmentInternalAsync(id, file, description, userId);
            if (!string.IsNullOrEmpty(err))
                return Json(new { success = false, message = err });
            await _context.SaveChangesAsync();

            var attachment = await _context.Attachments
                .Where(a => a.RefType == RefType && a.RefID == id)
                .OrderByDescending(a => a.UploadedAt)
                .FirstOrDefaultAsync();

            return Json(new
            {
                success = true,
                message = "Uploaded.",
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

        [HttpGet]
        public async Task<IActionResult> GetAttachments(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return Json(new { success = false, message = "Access denied." });

            var list = await _context.Attachments.AsNoTracking()
                .Where(a => a.RefType == RefType && a.RefID == id)
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

            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a => a.AttachmentID == attachmentId && a.RefType == RefType);
            if (attachment == null)
                return Json(new { success = false, message = "Attachment not found." });

            var row = await _context.DuplicateFileTransfers.FirstOrDefaultAsync(d => d.Id == attachment.RefID);
            if (row != null && !string.Equals(row.Status, Initiated, StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Cannot delete attachment after workflow decision." });

            if (!string.IsNullOrEmpty(attachment.FilePath))
            {
                var trimmed = attachment.FilePath.TrimStart('~').TrimStart('/');
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trimmed.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Deleted." });
        }

        private async Task<string?> SaveAttachmentInternalAsync(string refId, IFormFile file, string? description, string? uploadedByRaw)
        {
            if (file == null || file.Length == 0)
                return "No file.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedAttachmentExtensions.Contains(ext))
                return "Only JPG, PNG, GIF, BMP, PDF allowed.";
            if (file.Length > MaxAttachmentSize)
                return "Max file size 8MB.";

            var safeId = SanitizePathSegment(refId);
            if (string.IsNullOrEmpty(safeId))
                return "Invalid ID.";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "duplicatefiletransfers", safeId);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var relativePath = $"/uploads/duplicatefiletransfers/{safeId}/{uniqueFileName}";
            var attachmentId = Guid.NewGuid().ToString("N")[..10].ToUpper();
            var uploadedBy = string.IsNullOrEmpty(uploadedByRaw) ? null : TruncateUserId(uploadedByRaw);

            _context.Attachments.Add(new Attachment
            {
                AttachmentID = attachmentId,
                RefType = RefType,
                RefID = refId,
                AttachmentType = "Other",
                FileName = file.FileName,
                FilePath = relativePath,
                FileSize = file.Length,
                FileType = file.ContentType,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.Now
            });
            return null;
        }
    }
}
