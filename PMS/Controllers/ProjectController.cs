using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PMS.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private const string ModuleKey = "Project";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public ProjectController(PMSDbContext context, IModulePermissionService modulePermission)
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

        public async Task<IActionResult> Index()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var projects = await _context.Projects
                .Include(p => p.Properties)
                .Include(p => p.PaymentPlans)
                .Include(p => p.Ballotings)
                .ToListAsync();
            
            // Get customer counts for each project
            var projectIds = projects.Select(p => p.ProjectID).ToList();
            var customerCounts = await _context.Customers
                .Where(c => c.ProjectID != null && projectIds.Contains(c.ProjectID))
                .GroupBy(c => c.ProjectID!)
                .Select(g => new { ProjectID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProjectID, x => x.Count);
            
            ViewBag.CustomerCounts = customerCounts;
            
            return View(projects);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Properties)
                .Include(p => p.PaymentPlans)
                .Include(p => p.Ballotings)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            var mappings = await _context.ProjectSubProjects
                .AsNoTracking()
                .Where(s => s.ProjectID == id)
                .OrderBy(s => s.SubProjectName)
                .ToListAsync();
            ViewBag.SubProjectMappings = mappings;

            return View(project);
        }

        private List<string> GetProjectTypes(string? includeType = null)
        {
            var projectTypesConfig = _context.Configurations
                .FirstOrDefault(c => c.ConfigKey == "projecttypes");
            var types = projectTypesConfig != null
                ? projectTypesConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string> { "Residential", "Commercial", "Mixed", "Industrial" };

            if (!string.IsNullOrWhiteSpace(includeType)
                && !types.Any(t => string.Equals(t, includeType, StringComparison.OrdinalIgnoreCase)))
            {
                types.Insert(0, includeType.Trim());
            }

            return types;
        }

        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            ViewBag.ProjectTypes = GetProjectTypes();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            // Project-level prefix is now optional (legacy compatibility only).
            if (!string.IsNullOrEmpty(project.Prefix))
            {
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Prefix.ToUpper() == project.Prefix.ToUpper());
                
                if (existingProject != null)
                {
                    ModelState.AddModelError("Prefix", "A project with this Prefix already exists. Please use a different Prefix.");
                }
            }

            var subProjectMappings = ParseSubProjectMappings(project.SubProjects, ModelState, nameof(project.SubProjects));
            project.SubProjects = string.Join(",", subProjectMappings.Select(m => m.Name));

            if (ModelState.IsValid)
            {
                project.ProjectID = GenerateID();
                project.CreatedAt = DateTime.Now;

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                await ReplaceProjectSubProjectsAsync(project.ProjectID, subProjectMappings);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivity(userId, "Create Project", "Project", project.ProjectID);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProjectTypes = GetProjectTypes(project.Type);
            return View(project);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            var mappings = await _context.ProjectSubProjects
                .AsNoTracking()
                .Where(s => s.ProjectID == id)
                .OrderBy(s => s.SubProjectName)
                .Select(s => s.SubProjectName + "|" + s.Prefix)
                .ToListAsync();
            if (mappings.Count > 0)
            {
                project.SubProjects = string.Join(",", mappings);
            }
            else if (!string.IsNullOrWhiteSpace(project.SubProjects) && !string.IsNullOrWhiteSpace(project.Prefix))
            {
                var legacyNames = project.SubProjects
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => $"{s}|{project.Prefix!.Trim().ToUpperInvariant()}");
                project.SubProjects = string.Join(",", legacyNames);
            }

            ViewBag.ProjectTypes = GetProjectTypes(project.Type);
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Project project)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id != project.ProjectID)
            {
                return NotFound();
            }

            // Project-level prefix is now optional (legacy compatibility only).
            if (!string.IsNullOrEmpty(project.Prefix))
            {
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Prefix.ToUpper() == project.Prefix.ToUpper() && p.ProjectID != project.ProjectID);
                
                if (existingProject != null)
                {
                    ModelState.AddModelError("Prefix", "A project with this Prefix already exists. Please use a different Prefix.");
                }
            }

            var subProjectMappings = ParseSubProjectMappings(project.SubProjects, ModelState, nameof(project.SubProjects));
            project.SubProjects = string.Join(",", subProjectMappings.Select(m => m.Name));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                    await ReplaceProjectSubProjectsAsync(project.ProjectID, subProjectMappings);

                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await LogActivity(userId, "Update Project", "Project", project.ProjectID);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProjectTypes = GetProjectTypes(project.Type);
            return View(project);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivity(userId, "Delete Project", "Project", id);
                }

                TempData["Success"] = "Project deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ConductBalloting(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.Project = project;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConductBalloting(string projectId, string remarks)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(projectId))
            {
                return BadRequest();
            }

            var balloting = new Balloting
            {
                BallotID = GenerateID(),
                ProjectID = projectId,
                ConductedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                ConductedAt = DateTime.Now,
                Remarks = remarks
            };

            _context.Ballotings.Add(balloting);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Conduct Balloting", "Balloting", balloting.BallotID);
            }

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        /// <summary>Floor Plan: show all projects; optional sub-project filter after selecting a project.</summary>
        [HttpGet]
        public async Task<IActionResult> FloorPlan(string? projectId, string? subProject)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var allProjects = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.Projects = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                allProjects,
                "ProjectID",
                "ProjectName",
                projectId);

            var selectedProjectMeta = allProjects.FirstOrDefault(p => p.ProjectID == projectId);
            var subProjectOptions = (selectedProjectMeta?.SubProjects ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
            ViewBag.SubProjects = subProjectOptions;

            if (string.IsNullOrEmpty(projectId) || selectedProjectMeta == null)
            {
                return View(new FloorPlanViewModel
                {
                    SelectedProjectID = projectId,
                    SelectedSubProject = subProject
                });
            }

            var project = await _context.Projects
                .Include(p => p.Properties)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);
            if (project == null)
                return NotFound();

            var selectedSubProject = string.IsNullOrWhiteSpace(subProject) ? null : subProject.Trim();
            var propertiesByFloor = project.Properties
                .Where(p => selectedSubProject == null
                    || string.Equals((p.SubProject ?? string.Empty).Trim(), selectedSubProject, StringComparison.OrdinalIgnoreCase))
                .GroupBy(p =>
                {
                    var floor = (p.Floor ?? string.Empty).Trim();
                    return string.IsNullOrWhiteSpace(floor) ? "No floor mentioned" : floor;
                })
                .Select(g => new FloorGroupViewModel
                {
                    FloorName = g.Key,
                    Properties = g
                        .OrderBy(p => p.PlotNo ?? string.Empty)
                        .ThenBy(p => p.PropertyID)
                        .ToList()
                })
                .OrderBy(f => GetFloorSortOrder(f.FloorName))
                .ThenBy(f => f.FloorName)
                .ToList();

            var model = new FloorPlanViewModel
            {
                Project = project,
                SelectedProjectID = projectId,
                SelectedSubProject = selectedSubProject,
                Floors = propertiesByFloor
            };

            return View(model);
        }

        private bool ProjectExists(string id)
        {
            return _context.Projects.Any(e => e.ProjectID == id);
        }

        private static int GetFloorSortOrder(string? floorName)
        {
            var raw = (floorName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "No floor mentioned", StringComparison.OrdinalIgnoreCase))
                return 10000;

            var normalized = raw.ToLowerInvariant().Replace("-", " ").Replace("_", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            // Desired sequence: Basement -> Lower Ground -> Ground -> 1st -> 2nd -> ...
            if (normalized == "basement" || normalized == "bsmt")
                return -100;
            if (normalized == "lower ground" || normalized == "lg")
                return -50;
            if (normalized == "ground" || normalized == "g" || normalized == "gf" || normalized == "ground floor")
                return 0;

            // Parse ordinals / numeric floors like "24th", "26th", "12", "Floor 12", etc.
            var match = Regex.Match(normalized, @"\d+");
            if (match.Success && int.TryParse(match.Value, out var n))
                return n;

            // Unknown text floors come after numbered floors and before "No floor mentioned".
            return 9000;
        }

        private sealed class SubProjectPrefixMapping
        {
            public string Name { get; set; } = string.Empty;
            public string Prefix { get; set; } = string.Empty;
        }

        private static List<SubProjectPrefixMapping> ParseSubProjectMappings(
            string? rawCsv,
            ModelStateDictionary modelState,
            string modelKey)
        {
            var list = new List<SubProjectPrefixMapping>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var items = (rawCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            if (items.Count == 0)
            {
                modelState.AddModelError(modelKey, "At least one subproject with prefix is required.");
                return list;
            }

            foreach (var item in items)
            {
                var parts = item.Split('|', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    modelState.AddModelError(modelKey, $"Invalid subproject format: '{item}'. Use Name|Prefix (e.g., Phase 1|P1).");
                    continue;
                }

                var name = parts[0].Trim();
                var prefix = parts[1].Trim().ToUpperInvariant();
                if (prefix.Length > 7)
                {
                    modelState.AddModelError(modelKey, $"Prefix '{prefix}' exceeds max length 7.");
                    continue;
                }

                if (!seenNames.Add(name))
                {
                    modelState.AddModelError(modelKey, $"Duplicate subproject name: '{name}'.");
                    continue;
                }
                if (!seenPrefixes.Add(prefix))
                {
                    modelState.AddModelError(modelKey, $"Duplicate subproject prefix: '{prefix}'.");
                    continue;
                }

                list.Add(new SubProjectPrefixMapping { Name = name, Prefix = prefix });
            }

            return list;
        }

        private async Task ReplaceProjectSubProjectsAsync(string? projectId, List<SubProjectPrefixMapping> mappings)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return;

            var existing = await _context.ProjectSubProjects
                .Where(s => s.ProjectID == projectId)
                .ToListAsync();
            if (existing.Count > 0)
                _context.ProjectSubProjects.RemoveRange(existing);

            var rows = mappings.Select(m => new ProjectSubProject
            {
                Id = GenerateID(),
                ProjectID = projectId,
                SubProjectName = m.Name,
                Prefix = m.Prefix,
                CreatedAt = DateTime.Now
            }).ToList();

            if (rows.Count > 0)
                _context.ProjectSubProjects.AddRange(rows);

            await _context.SaveChangesAsync();
        }

        private async Task LogActivity(string userId, string action, string refType, string refId)
        {
            var activityLog = new ActivityLog
            {
                UserID = userId,
                Action = action,
                RefType = refType,
                RefID = refId,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }

        private string GenerateID()
        {
            return Guid.NewGuid().ToString("N")[..10].ToUpper();
        }
    }
}
