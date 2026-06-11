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
    public class TransferFeeController : Controller
    {
        private const string ModuleKey = "TransferFee";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public TransferFeeController(PMSDbContext context, IModulePermissionService modulePermission)
        {
            _context = context;
            _modulePermission = modulePermission;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);

            // Backward-compatible fallback so existing users with Transfer access can use this module.
            if (perm == "NoAccess")
            {
                perm = await _modulePermission.GetPermissionAsync(userId, "Transfer");
            }

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

        [HttpGet]
        public async Task<IActionResult> Index(string projectFilter = "", string subProjectFilter = "", string priorityFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var query = _context.TransferFees
                .Include(t => t.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(projectFilter))
                query = query.Where(t => t.ProjectID == projectFilter);

            if (!string.IsNullOrWhiteSpace(subProjectFilter))
            {
                var sub = subProjectFilter.Trim();
                query = query.Where(t => (t.SubProject ?? "").Trim() == sub);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter))
                query = query.Where(t => t.TransferPriority == priorityFilter);

            ViewBag.Projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
            ViewBag.ProjectFilter = projectFilter;
            ViewBag.SubProjectFilter = subProjectFilter;
            ViewBag.PriorityFilter = priorityFilter;

            var list = await query
                .OrderBy(t => t.Project!.ProjectName)
                .ThenBy(t => t.SubProject)
                .ThenBy(t => t.TransferType)
                .ThenBy(t => t.TransferPriority)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = await _context.TransferFees
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? projectId = null, string? subProject = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            ViewBag.Projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
            ViewBag.SubProjects = await GetSubProjectsForProjectAsync(projectId);
            ViewBag.TransferTypes = await GetTransferTypesAsync();

            var model = new TransferFee
            {
                ProjectID = projectId?.Trim() ?? string.Empty,
                SubProject = subProject?.Trim(),
                TransferPriority = "Normal",
                TransferType = "Normal Transfer",
                CreatedOn = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferFee model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            model.ProjectID = (model.ProjectID ?? string.Empty).Trim();
            model.SubProject = string.IsNullOrWhiteSpace(model.SubProject) ? null : model.SubProject.Trim();
            model.TransferType = string.IsNullOrWhiteSpace(model.TransferType) ? null : model.TransferType.Trim();
            model.TransferPriority = string.IsNullOrWhiteSpace(model.TransferPriority) ? null : model.TransferPriority.Trim();

            if (string.IsNullOrWhiteSpace(model.ProjectID))
                ModelState.AddModelError(nameof(model.ProjectID), "Project is required.");
            if (string.IsNullOrWhiteSpace(model.SubProject))
                ModelState.AddModelError(nameof(model.SubProject), "SubProject is required.");
            if (model.AmountPerUnit <= 0)
                ModelState.AddModelError(nameof(model.AmountPerUnit), "Amount per unit must be greater than zero.");

            var duplicateExists = await _context.TransferFees.AnyAsync(t =>
                t.ProjectID == model.ProjectID &&
                (t.SubProject ?? "") == (model.SubProject ?? "") &&
                (t.TransferType ?? "") == (model.TransferType ?? "") &&
                (t.TransferPriority ?? "") == (model.TransferPriority ?? ""));

            if (duplicateExists)
                ModelState.AddModelError("", "A transfer fee row already exists for this Project, SubProject, Transfer Type, and Priority.");

            if (!ModelState.IsValid)
            {
                ViewBag.Projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
                ViewBag.SubProjects = await GetSubProjectsForProjectAsync(model.ProjectID);
                ViewBag.TransferTypes = await GetTransferTypesAsync();
                return View(model);
            }

            model.Id = Guid.NewGuid().ToString("N")[..10].ToUpper();
            model.CreatedOn = DateTime.Now;
            model.CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            model.ModifiedBy = model.CreatedBy;

            _context.TransferFees.Add(model);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await LogActivityAsync(
                    userId,
                    $"Create Transfer Fee: {model.ProjectID} / {model.SubProject} / {model.TransferType} / {model.TransferPriority} / {model.AmountPerUnit:N2}",
                    "TransferFee",
                    model.Id);
            }

            TempData["Success"] = "Transfer fee created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = await _context.TransferFees.FirstOrDefaultAsync(t => t.Id == id);
            if (model == null)
                return NotFound();

            ViewBag.Projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
            ViewBag.SubProjects = await GetSubProjectsForProjectAsync(model.ProjectID);
            ViewBag.TransferTypes = await GetTransferTypesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TransferFee model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(id) || id != model.Id)
                return NotFound();

            var existing = await _context.TransferFees.FirstOrDefaultAsync(t => t.Id == id);
            if (existing == null)
                return NotFound();

            model.ProjectID = (model.ProjectID ?? string.Empty).Trim();
            model.SubProject = string.IsNullOrWhiteSpace(model.SubProject) ? null : model.SubProject.Trim();
            model.TransferType = string.IsNullOrWhiteSpace(model.TransferType) ? null : model.TransferType.Trim();
            model.TransferPriority = string.IsNullOrWhiteSpace(model.TransferPriority) ? null : model.TransferPriority.Trim();

            if (string.IsNullOrWhiteSpace(model.ProjectID))
                ModelState.AddModelError(nameof(model.ProjectID), "Project is required.");
            if (string.IsNullOrWhiteSpace(model.SubProject))
                ModelState.AddModelError(nameof(model.SubProject), "SubProject is required.");
            if (model.AmountPerUnit <= 0)
                ModelState.AddModelError(nameof(model.AmountPerUnit), "Amount per unit must be greater than zero.");

            var duplicateExists = await _context.TransferFees.AnyAsync(t =>
                t.Id != model.Id &&
                t.ProjectID == model.ProjectID &&
                (t.SubProject ?? "") == (model.SubProject ?? "") &&
                (t.TransferType ?? "") == (model.TransferType ?? "") &&
                (t.TransferPriority ?? "") == (model.TransferPriority ?? ""));

            if (duplicateExists)
                ModelState.AddModelError("", "A transfer fee row already exists for this Project, SubProject, Transfer Type, and Priority.");

            if (!ModelState.IsValid)
            {
                ViewBag.Projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
                ViewBag.SubProjects = await GetSubProjectsForProjectAsync(model.ProjectID);
                ViewBag.TransferTypes = await GetTransferTypesAsync();
                return View(model);
            }

            existing.ProjectID = model.ProjectID;
            existing.SubProject = model.SubProject;
            existing.TransferType = model.TransferType;
            existing.TransferPriority = model.TransferPriority;
            existing.AmountPerUnit = model.AmountPerUnit;
            existing.Details = model.Details;
            existing.ModifiedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await LogActivityAsync(
                    userId,
                    $"Edit Transfer Fee: {existing.ProjectID} / {existing.SubProject} / {existing.TransferType} / {existing.TransferPriority} / {existing.AmountPerUnit:N2}",
                    "TransferFee",
                    existing.Id);
            }

            TempData["Success"] = "Transfer fee updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var item = await _context.TransferFees.FirstOrDefaultAsync(t => t.Id == id);
            if (item == null)
            {
                TempData["Error"] = "Transfer fee row not found.";
                return RedirectToAction(nameof(Index));
            }

            var deletedId = item.Id;
            var deletedSummary = $"{item.ProjectID} / {item.SubProject} / {item.TransferType} / {item.TransferPriority} / {item.AmountPerUnit:N2}";
            _context.TransferFees.Remove(item);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await LogActivityAsync(
                    userId,
                    $"Delete Transfer Fee: {deletedSummary}",
                    "TransferFee",
                    deletedId);
            }

            TempData["Success"] = "Transfer fee row deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectSubProjects(string projectId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var values = await GetSubProjectsForProjectAsync(projectId);
            return Json(new { success = true, subProjects = values });
        }

        private async Task<List<string>> GetSubProjectsForProjectAsync(string? projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return new List<string>();

            var mapped = await _context.ProjectSubProjects
                .AsNoTracking()
                .Where(s => s.ProjectID == projectId)
                .OrderBy(s => s.SubProjectName)
                .Select(s => s.SubProjectName)
                .ToListAsync();
            if (mapped.Count > 0)
                return mapped;

            var csv = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.SubProjects)
                .FirstOrDefaultAsync();

            return (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
        }

        private async Task<List<string>> GetTransferTypesAsync()
        {
            var config = await _context.Configurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigKey == "NDCType");

            var types = (config?.ConfigValue ?? "Normal Transfer,Urgent Transfer,Family Transfer,Death Transfer")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            const string duplicateFile = "Duplicate File Transfer";
            if (!types.Any(t => string.Equals(t, duplicateFile, StringComparison.OrdinalIgnoreCase)))
                types.Add(duplicateFile);

            return types;
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max];
        }

        private async Task LogActivityAsync(string userId, string action, string refType, string refId)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID = Truncate(userId, 10),
                Action = Truncate(action, 255),
                RefType = Truncate(refType, 50),
                RefID = Truncate(refId, 10),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}
