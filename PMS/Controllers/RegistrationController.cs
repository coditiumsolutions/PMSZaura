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
    public class RegistrationController : Controller
    {
        private const string ModuleKey = "Registration";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public RegistrationController(PMSDbContext context, IModulePermissionService modulePermission)
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

        // GET: Registration/Index
        public async Task<IActionResult> Index(string statusFilter = "All", string searchTerm = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var registrationsQuery = _context.Registrations
                .Include(r => r.Customers)
                .Include(r => r.Project)
                .AsQueryable();

            // Apply status filter
            if (statusFilter != "All" && !string.IsNullOrEmpty(statusFilter))
            {
                registrationsQuery = registrationsQuery.Where(r => r.Status == statusFilter);
            }

            // Apply search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                registrationsQuery = registrationsQuery.Where(r =>
                    r.RegID.Contains(searchTerm) ||
                    r.FullName.Contains(searchTerm) ||
                    r.CNIC.Contains(searchTerm) ||
                    (r.FormNo != null && r.FormNo.Contains(searchTerm)) ||
                    r.Phone.Contains(searchTerm) ||
                    r.Email.Contains(searchTerm)
                );
            }

            var registrations = await registrationsQuery
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Statistics
            ViewBag.TotalRegistrations = await _context.Registrations.CountAsync();
            ViewBag.PendingRegistrations = await _context.Registrations.Where(r => r.Status == "Pending").CountAsync();
            ViewBag.ApprovedRegistrations = await _context.Registrations.Where(r => r.Status == "Approved").CountAsync();
            ViewBag.RejectedRegistrations = await _context.Registrations.Where(r => r.Status == "Rejected").CountAsync();
            ViewBag.ConvertedToCustomers = await _context.Customers.CountAsync();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchTerm = searchTerm;

            return View(registrations);
        }

        // GET: Registration/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .Include(r => r.Customers)
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.RegID == id);

            if (registration == null)
            {
                return NotFound();
            }

            return View(registration);
        }

        // GET: Registration/Create
        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.ProjectID, p.ProjectName })
                .ToListAsync();
            return View();
        }

        // POST: Registration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Registration registration)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    registration.RegID = await GenerateID();
                    registration.CreatedAt = DateTime.Now;
                    registration.Status = "Pending";

                    _context.Registrations.Add(registration);
                    await _context.SaveChangesAsync();

                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await LogActivity(userId, "Create Registration", "Registration", registration.RegID);
                    }

                    await transaction.CommitAsync();

                    TempData["Success"] = $"Registration created successfully! Registration ID: {registration.RegID}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Error creating registration: {ex.Message}";
                    return View(registration);
                }
            }

            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.ProjectID, p.ProjectName })
                .ToListAsync();
            return View(registration);
        }

        /// <summary>Returns sizes (CSV split) for a project. Used by Registration form to populate Size dropdown.</summary>
        [HttpGet]
        public async Task<IActionResult> GetProjectSizes(string projectId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Json(Array.Empty<string>());
            }
            var project = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.Sizes)
                .FirstOrDefaultAsync();
            var sizes = string.IsNullOrWhiteSpace(project)
                ? Array.Empty<string>()
                : project.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            return Json(sizes);
        }

        /// <summary>Returns subprojects (CSV split) for a project. Used by Registration form to populate SubProject dropdown.</summary>
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

            var project = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.SubProjects)
                .FirstOrDefaultAsync();
            var subProjects = string.IsNullOrWhiteSpace(project)
                ? Array.Empty<string>()
                : project.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            return Json(subProjects);
        }

        // GET: Registration/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
            {
                return NotFound();
            }

            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.ProjectID, p.ProjectName })
                .ToListAsync();
            return View(registration);
        }

        // POST: Registration/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Registration registration)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id != registration.RegID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(registration);
                    await _context.SaveChangesAsync();

                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await LogActivity(userId, "Update Registration", "Registration", registration.RegID);
                    }

                    TempData["Success"] = "Registration updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RegistrationExists(registration.RegID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.ProjectID, p.ProjectName })
                .ToListAsync();
            return View(registration);
        }

        // POST: Registration/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string regID, string status)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var registration = await _context.Registrations.FindAsync(regID);
            if (registration == null)
            {
                TempData["Error"] = "Registration not found";
                return RedirectToAction(nameof(Index));
            }

            registration.Status = status;
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, $"Update Registration Status to {status}", "Registration", regID);
            }

            TempData["Success"] = $"Registration status updated to {status}";
            return RedirectToAction(nameof(Details), new { id = regID });
        }

        // GET: Registration/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .Include(r => r.Customers)
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.RegID == id);

            if (registration == null)
            {
                return NotFound();
            }

            // Check if registration has customers
            if (registration.Customers != null && registration.Customers.Any())
            {
                TempData["Error"] = "Cannot delete registration. It is linked to customer(s).";
                return RedirectToAction(nameof(Index));
            }

            return View(registration);
        }

        // POST: Registration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var registration = await _context.Registrations
                .Include(r => r.Customers)
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.RegID == id);

            if (registration == null)
            {
                return NotFound();
            }

            // Double check if registration has customers
            if (registration.Customers != null && registration.Customers.Any())
            {
                TempData["Error"] = "Cannot delete registration. It is linked to customer(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Delete Registration", "Registration", id);
            }

            TempData["Success"] = "Registration deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool RegistrationExists(string id)
        {
            return _context.Registrations.Any(e => e.RegID == id);
        }

        private async Task<string> GenerateID()
        {
            string newId;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                attempts++;
                
                // Get the maximum numeric ID from existing registrations
                var existingIds = await _context.Registrations
                    .Where(r => r.RegID.StartsWith("REG") && r.RegID.Length == 10)
                    .Select(r => r.RegID.Substring(3))
                    .ToListAsync();

                var maxNumericId = existingIds
                    .Where(id => int.TryParse(id, out _))
                    .Select(id => int.Parse(id))
                    .DefaultIfEmpty(0)
                    .Max();

                int nextId = maxNumericId + 1;
                newId = "REG" + nextId.ToString("D7");

                // Check if this ID already exists (race condition protection)
                if (!_context.Registrations.Any(r => r.RegID == newId))
                {
                    break;
                }

                // If we've tried too many times, add a random component
                if (attempts >= maxAttempts)
                {
                    var random = new Random();
                    newId = "REG" + (maxNumericId + random.Next(1, 1000)).ToString("D7");
                    break;
                }

            } while (attempts < maxAttempts);

            return newId;
        }

        private async Task LogActivity(string userId, string action, string refType, string refId)
        {
            var log = new ActivityLog
            {
                UserID = userId,
                Action = action,
                RefType = refType,
                RefID = refId,
                CreatedAt = DateTime.Now
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}

