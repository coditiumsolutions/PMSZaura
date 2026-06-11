using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;

namespace PMS.Controllers
{
    [Authorize]
    public class PropertyController : Controller
    {
        private const string ModuleKey = "Property";
        private readonly PMSDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IModulePermissionService _modulePermission;

        public PropertyController(PMSDbContext context, IWebHostEnvironment environment, IModulePermissionService modulePermission)
        {
            _context = context;
            _environment = environment;
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

        /// <summary>Customer must match property: same ProjectID, SubProject, and customer RegisteredSize vs property Size (trimmed, case-insensitive).</summary>
        private static bool CustomerMatchesPropertyForAllotment(Customer c, Property p)
        {
            if (string.IsNullOrWhiteSpace(p.ProjectID))
                return false;
            if (!string.Equals((c.ProjectID ?? "").Trim(), (p.ProjectID ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals((c.SubProject ?? "").Trim(), (p.SubProject ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals((c.RegisteredSize ?? "").Trim(), (p.Size ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private static readonly string[] AllowedAllotmentAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private const long MaxAllotmentAttachmentSize = 8 * 1024 * 1024;

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

        private static string? TruncateUploadedBy(string? userId) =>
            string.IsNullOrEmpty(userId) ? null : (userId.Length <= 10 ? userId : userId[..10]);

        private static string NormalizeNumericToTwoDecimals(string? raw)
        {
            var s = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            // If it's a pure number, truncate to 2 decimals (don't round).
            // Accept both invariant and current culture decimal formats.
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ||
                decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out d))
            {
                var truncated = Math.Truncate(d * 100m) / 100m;
                if (truncated == Math.Truncate(truncated))
                    return truncated.ToString("0", CultureInfo.InvariantCulture);
                return truncated.ToString("0.00", CultureInfo.InvariantCulture);
            }

            return s;
        }

        public async Task<IActionResult> Index(string projectFilter = "All", string statusFilter = "", string searchTerm = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var query = _context.Properties
                .Include(p => p.Project)
                .Include(p => p.Allotments)
                    .ThenInclude(a => a.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(projectFilter) && !string.Equals(projectFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectID == projectFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(p => p.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p =>
                    p.PropertyID.Contains(term) ||
                    (p.PlotNo != null && p.PlotNo.Contains(term)) ||
                    (p.Block != null && p.Block.Contains(term)));
            }

            var properties = await query.ToListAsync();
            ViewBag.Projects = await _context.Projects
                .AsNoTracking()
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.ProjectID, p.ProjectName })
                .ToListAsync();
            ViewBag.ProjectFilter = projectFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.AvailableCount = await _context.Properties.CountAsync(p => p.Status == "Available");
            ViewBag.AllottedCount = await _context.Properties.CountAsync(p => p.Status == "Allotted");
            ViewBag.VacantCount = await _context.Properties.CountAsync(p => p.Status == "Vacant");
            ViewBag.OccupiedCount = await _context.Properties.CountAsync(p => p.Status == "Occupied");
            return View(properties);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Project)
                .Include(p => p.Allotments)
                    .ThenInclude(a => a.Customer)
                .Include(p => p.Possessions)
                    .ThenInclude(po => po.Customer)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            ViewBag.Projects = _context.Projects.ToList();
            ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").ToList();

            return View();
        }

        /// <summary>Returns SubProjects and Sizes from the Projects row (for Property Create/Edit). Property types come from Configurations (PropertyType key).</summary>
        [HttpGet]
        public IActionResult GetProjectDetails(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return Json(new { subProjects = Array.Empty<string>(), sizes = Array.Empty<string>() });

            var project = _context.Projects.AsNoTracking().FirstOrDefault(p => p.ProjectID == projectId);
            if (project == null)
                return Json(new { subProjects = Array.Empty<string>(), sizes = Array.Empty<string>() });

            // New structure stores subprojects in ProjectSubProjects (Name + Prefix mapping).
            // UI dropdowns should show just the SubProjectName values.
            var mappedSubProjects = _context.ProjectSubProjects
                .AsNoTracking()
                .Where(s => s.ProjectID == projectId)
                .Select(s => s.SubProjectName)
                .ToList();

            var subProjectsFromMapping = mappedSubProjects
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToArray();

            // Legacy fallback: older Projects rows may still store SubProjects as CSV.
            var subProjectsLegacy = (project.SubProjects ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToArray();

            var subProjects = subProjectsFromMapping.Length > 0 ? subProjectsFromMapping : subProjectsLegacy;
            var sizes = (project.Sizes ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            return Json(new { subProjects, sizes });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property)
        {
            if (ModelState.IsValid)
            {
                property.PropertyID = GenerateID();
                property.CreatedAt = DateTime.Now;
                property.Status = "Available";

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivity(userId, "Create Property", "Property", property.PropertyID);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Projects = _context.Projects.ToList();
            ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").ToList();
            return View(property);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            ViewBag.Projects = _context.Projects.ToList();
            ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").ToList();
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Property property)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id != property.PropertyID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(property);
                    await _context.SaveChangesAsync();

                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await LogActivity(userId, "Update Property", "Property", property.PropertyID);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.PropertyID))
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

            ViewBag.Projects = _context.Projects.ToList();
            ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").ToList();

            return View(property);
        }

        [HttpGet]
        public async Task<IActionResult> Allot(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Project)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            var candidates = string.IsNullOrWhiteSpace(property.ProjectID)
                ? new List<Customer>()
                : await _context.Customers.AsNoTracking()
                    .Where(c => c.ProjectID != null && c.ProjectID == property.ProjectID)
                    .ToListAsync();
            var customers = candidates
                .Where(c => CustomerMatchesPropertyForAllotment(c, property))
                .OrderBy(c => c.FullName)
                .ToList();
            ViewBag.Customers = customers;
            ViewBag.MatchingCustomerCount = customers.Count;
            ViewBag.Property = property;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
        public async Task<IActionResult> Allot(string propertyId, string customerId, string allotmentType, string comments, [FromForm] List<IFormFile>? attachmentFiles)
        {
            if (string.IsNullOrEmpty(propertyId) || string.IsNullOrEmpty(customerId))
            {
                return BadRequest();
            }

            var property = await _context.Properties.FindAsync(propertyId);
            var customer = await _context.Customers.FindAsync(customerId);
            if (property == null || customer == null)
            {
                return BadRequest();
            }

            if (!CustomerMatchesPropertyForAllotment(customer, property))
            {
                TempData["Error"] = "Selected customer must match this property's project, subproject, and size (same as the customer's registered size).";
                return RedirectToAction(nameof(Allot), new { id = propertyId });
            }

            if (string.IsNullOrWhiteSpace(allotmentType))
            {
                TempData["Error"] = "Please select an allotment type.";
                return RedirectToAction(nameof(Allot), new { id = propertyId });
            }

            var filesToPersist = new List<IFormFile>();
            if (attachmentFiles != null)
            {
                foreach (var file in attachmentFiles.Where(f => f != null && f.Length > 0))
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!AllowedAllotmentAttachmentExtensions.Contains(ext))
                    {
                        TempData["Error"] = $"Attachments: invalid file type for \"{file.FileName}\". Only JPG, PNG, GIF, BMP, and PDF are allowed.";
                        return RedirectToAction(nameof(Allot), new { id = propertyId });
                    }

                    if (file.Length > MaxAllotmentAttachmentSize)
                    {
                        TempData["Error"] = $"Attachments: \"{file.FileName}\" exceeds the 8 MB size limit.";
                        return RedirectToAction(nameof(Allot), new { id = propertyId });
                    }

                    filesToPersist.Add(file);
                }
            }

            var allotmentId = GenerateID();
            // Workflow: Initiated -> Operations Desk -> Approved/Declined
            var allotment = new Allotment
            {
                AllotmentID = allotmentId,
                PropertyID = propertyId,
                CustomerID = customerId,
                AllottedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                AllotmentDate = DateTime.Now,
                AllottmentType = allotmentType,
                WorkFlowStatus = "Initiated",
                Comments = comments
            };

            _context.Allotments.Add(allotment);

            // Reserve until approved.
            property.Status = "Reserved";
            _context.Update(property);

            await _context.SaveChangesAsync();

            if (filesToPersist.Count > 0)
            {
                var safeId = SanitizePathSegment(allotmentId);
                if (string.IsNullOrEmpty(safeId))
                {
                    TempData["Warning"] = "Allotment was saved, but attachments could not be stored (invalid allotment id for file path).";
                }
                else
                {
                    var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsFolder = Path.Combine(webRoot, "uploads", "allotments", safeId);
                    Directory.CreateDirectory(uploadsFolder);
                    var uploadedBy = TruncateUploadedBy(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    var newAttachments = new List<Attachment>();

                    foreach (var file in filesToPersist)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                        var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);
                        await using (var stream = new FileStream(physicalPath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        var relativePath = $"/uploads/allotments/{safeId}/{uniqueFileName}";
                        newAttachments.Add(new Attachment
                        {
                            AttachmentID = GenerateID(),
                            RefType = "Allotment",
                            RefID = allotmentId,
                            AttachmentType = "Allotment",
                            FileName = file.FileName,
                            FilePath = relativePath,
                            FileSize = file.Length,
                            FileType = file.ContentType,
                            Description = null,
                            UploadedBy = uploadedBy,
                            UploadedAt = DateTime.Now
                        });
                    }

                    _context.Attachments.AddRange(newAttachments);
                    await _context.SaveChangesAsync();
                }
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Allot Property", "Allotment", allotment.AllotmentID);
            }

            return RedirectToAction(nameof(Details), new { id = propertyId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Project)
                .Include(p => p.Allotments)
                    .ThenInclude(a => a.Customer)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivity(userId, "Delete Property", "Property", id);
                }

                TempData["Success"] = "Property deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PropertyExists(string id)
        {
            return _context.Properties.Any(e => e.PropertyID == id);
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

        public IActionResult Import()
        {
            ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
            ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
            return View();
        }

        public IActionResult DownloadSample()
        {
            try
            {
                // Look for the file in wwwroot/samples (standard location for static files)
                // This ensures the file is included when publishing
                var filePath = Path.Combine(_environment.WebRootPath, "samples", "property.xlsx");

                // Fallback to other locations for development
                if (!System.IO.File.Exists(filePath))
                {
                    var possiblePaths = new[]
                    {
                        Path.Combine(_environment.WebRootPath, "samples", "property.xlsx"),
                        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "samples", "property.xlsx"),
                        Path.Combine(_environment.ContentRootPath, "wwwroot", "samples", "property.xlsx"),
                        Path.Combine(Directory.GetCurrentDirectory(), "bin", "property.xlsx"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "property.xlsx")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            filePath = path;
                            break;
                        }
                    }
                }

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "Sample file not found. Please contact administrator.";
                    return RedirectToAction(nameof(Import));
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileName = "property_sample.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error downloading sample file: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPreview(IFormFile file, int dealerId, string projectId)
        {
            ViewBag.SelectedDealerID = dealerId;
            ViewBag.SelectedProjectID = projectId;

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
                ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
                return View("Import");
            }

            if (dealerId <= 0)
            {
                TempData["Error"] = "Please select a dealer.";
                ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
                ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
                ViewBag.SubProjects = new List<string>();
                return View("Import");
            }

            // Verify dealer exists
            var dealer = await _context.Dealers.FindAsync(dealerId);
            if (dealer == null)
            {
                TempData["Error"] = "Selected dealer not found.";
                ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
                ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
                return View("Import");
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                TempData["Error"] = "Please select a project.";
                ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
                ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
                return View("Import");
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectID == projectId);
            if (project == null)
            {
                TempData["Error"] = "Selected project not found.";
                ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
                ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
                return View("Import");
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
            {
                TempData["Error"] = "Please upload a CSV or Excel file (.csv, .xlsx, .xls).";
                return RedirectToAction(nameof(Import));
            }

            List<PropertyImportViewModel> properties = new List<PropertyImportViewModel>();

            try
            {
                if (extension == ".csv")
                {
                    properties = ParseCsvFile(file);
                }
                else
                {
                    properties = ParseExcelFile(file);
                }

                if (properties.Count == 0)
                {
                    TempData["Error"] = "No valid data found in the file. Please check the file format.";
                    return RedirectToAction(nameof(Import));
                }

                // Hard gate: validate that every subproject in file exists in ProjectSubProjects
                // and also has a prefix set, before allowing preview/import.
                var subProjectMappings = await _context.ProjectSubProjects
                    .AsNoTracking()
                    .Where(s => s.ProjectID == projectId)
                    .Select(s => new { s.SubProjectName, s.Prefix })
                    .ToListAsync();

                var mappingByName = subProjectMappings
                    .Where(m => !string.IsNullOrWhiteSpace(m.SubProjectName))
                    .GroupBy(m => m.SubProjectName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Prefix, StringComparer.OrdinalIgnoreCase);

                var distinctFileSubProjects = properties
                    .Select(p => (p.SubProject ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                var missingSubProjects = distinctFileSubProjects
                    .Where(sp => !mappingByName.ContainsKey(sp))
                    .ToList();

                var missingPrefix = distinctFileSubProjects
                    .Where(sp =>
                    {
                        if (!mappingByName.TryGetValue(sp, out var prefix)) return false;
                        return string.IsNullOrWhiteSpace(prefix);
                    })
                    .ToList();

                if (missingSubProjects.Count > 0 || missingPrefix.Count > 0)
                {
                    TempData["Error"] = "Import blocked: some SubProjects in the file are missing (or missing prefix) in the selected Project's SubProject mappings.";
                    ViewBag.Dealers = _context.Dealers.Where(d => d.Status == "Active").OrderBy(d => d.DealershipName).ToList();
                    ViewBag.Projects = _context.Projects.OrderBy(p => p.ProjectName).ToList();
                    ViewBag.MissingSubProjects = missingSubProjects;
                    ViewBag.SubProjectsMissingPrefix = missingPrefix;
                    return View("Import");
                }

                foreach (var property in properties)
                {
                    property.ProjectID = projectId;
                    var validationErrors = new List<string>();
                    if (string.IsNullOrWhiteSpace(property.SubProject))
                        validationErrors.Add("SubProject is required");
                    if (string.IsNullOrWhiteSpace(property.Block))
                        validationErrors.Add("Block is required");
                    if (string.IsNullOrWhiteSpace(property.Size))
                        validationErrors.Add("Size is required");
                    if (string.IsNullOrWhiteSpace(property.PlotNo))
                        validationErrors.Add("Unit No is required");
                    if (string.IsNullOrWhiteSpace(property.PlotType))
                        validationErrors.Add("Unit type (category) is required");

                    if (validationErrors.Count > 0)
                    {
                        property.IsValid = false;
                        property.ErrorMessage = string.Join(", ", validationErrors);
                    }
                }

                ViewBag.TotalRows = properties.Count;
                ViewBag.ValidRows = properties.Count(p => p.IsValid);
                ViewBag.InvalidRows = properties.Count(p => !p.IsValid);
                ViewBag.FileName = file.FileName;
                ViewBag.ProjectName = project.ProjectName;
                ViewBag.ProjectID = project.ProjectID;

                // Store in session for confirmation
                HttpContext.Session.SetString("ImportData", System.Text.Json.JsonSerializer.Serialize(properties));
                HttpContext.Session.SetInt32("ImportDealerID", dealerId);
                HttpContext.Session.SetString("ImportProjectID", projectId);
                ViewBag.DealerName = dealer.DealershipName;

                return View(properties);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing file: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportConfirm()
        {
            var importDataJson = HttpContext.Session.GetString("ImportData");
            var dealerId = HttpContext.Session.GetInt32("ImportDealerID");
            var projectId = HttpContext.Session.GetString("ImportProjectID");
            
            if (string.IsNullOrEmpty(importDataJson))
            {
                TempData["Error"] = "Import session expired. Please upload the file again.";
                return RedirectToAction(nameof(Import));
            }

            if (dealerId == null || dealerId <= 0)
            {
                TempData["Error"] = "Dealer information not found. Please upload the file again.";
                return RedirectToAction(nameof(Import));
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                TempData["Error"] = "Project information not found. Please upload the file again.";
                return RedirectToAction(nameof(Import));
            }

            var projectExists = await _context.Projects.AnyAsync(p => p.ProjectID == projectId);
            if (!projectExists)
            {
                TempData["Error"] = "Selected project no longer exists. Please upload the file again.";
                return RedirectToAction(nameof(Import));
            }

            var properties = System.Text.Json.JsonSerializer.Deserialize<List<PropertyImportViewModel>>(importDataJson);
            if (properties == null || properties.Count == 0)
            {
                TempData["Error"] = "No data to import.";
                return RedirectToAction(nameof(Import));
            }

            var validProperties = properties.Where(p => p.IsValid).ToList();
            if (validProperties.Count == 0)
            {
                TempData["Error"] = "No valid properties to import.";
                return RedirectToAction(nameof(Import));
            }

            // Re-check mappings on confirm (protect against race/config changes)
            var subProjectMappings = await _context.ProjectSubProjects
                .AsNoTracking()
                .Where(s => s.ProjectID == projectId)
                .Select(s => new { s.SubProjectName, s.Prefix })
                .ToListAsync();
            var mappingByName = subProjectMappings
                .Where(m => !string.IsNullOrWhiteSpace(m.SubProjectName))
                .GroupBy(m => m.SubProjectName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Prefix, StringComparer.OrdinalIgnoreCase);
            var distinctFileSubProjects = validProperties
                .Select(p => (p.SubProject ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinctFileSubProjects.Any(sp => !mappingByName.ContainsKey(sp) || string.IsNullOrWhiteSpace(mappingByName[sp]!)))
            {
                TempData["Error"] = "Import blocked: SubProject mappings changed or are incomplete. Please update Project SubProjects and upload again.";
                return RedirectToAction(nameof(Import));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int importedCount = 0;
            int skippedCount = 0;

            foreach (var prop in validProperties)
            {
                try
                {
                    // Check if property already exists (by ProjectID + PlotNo + Block)
                    var exists = await _context.Properties
                        .AnyAsync(p => p.ProjectID == projectId && 
                                      p.PlotNo == prop.PlotNo && 
                                      p.Block == prop.Block);

                    if (exists)
                    {
                        skippedCount++;
                        continue;
                    }

                    var property = new Property
                    {
                        PropertyID = GenerateID(),
                        ProjectID = projectId!,
                        SubProject = prop.SubProject,
                        PlotNo = prop.PlotNo,
                        Street = prop.Street,
                        PlotType = prop.PlotType,
                        Block = prop.Block,
                        Floor = prop.Floor,
                        PropertyType = null,
                        Size = prop.Size,
                        AdditionalInfo = prop.AdditionalInfo,
                        Status = "Available",
                        DealerID = dealerId.Value,
                        CreatedAt = DateTime.Now
                    };

                    _context.Properties.Add(property);
                    importedCount++;
                }
                catch (Exception)
                {
                    skippedCount++;
                    continue;
                }
            }

            await _context.SaveChangesAsync();

            if (userId != null)
            {
                await LogActivity(userId, $"Import Properties - {importedCount} imported", "Property", "Bulk");
            }

            HttpContext.Session.Remove("ImportData");
            HttpContext.Session.Remove("ImportProjectID");

            TempData["Success"] = $"Successfully imported {importedCount} properties. {skippedCount} skipped (duplicates or errors).";
            return RedirectToAction(nameof(Index));
        }

        private List<PropertyImportViewModel> ParseCsvFile(IFormFile file)
        {
            var properties = new List<PropertyImportViewModel>();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string? headerLine = reader.ReadLine();
                if (headerLine == null) return properties;
                var headerColumns = ParseCsvLine(headerLine);
                var hasProjectColumn = headerColumns.Count > 0 &&
                                       string.Equals(headerColumns[0]?.Trim(), "ProjectID", StringComparison.OrdinalIgnoreCase);
                var startIndex = hasProjectColumn ? 1 : 0;
                var hasSubProjectColumn = headerColumns.Count > startIndex &&
                                          string.Equals((headerColumns[startIndex] ?? string.Empty).Trim(), "SubProject", StringComparison.OrdinalIgnoreCase);
                // New format requires SubProject as first data column (after optional ProjectID).
                var dataStartIndex = startIndex + 1;
                // Header names can vary (e.g. "Sub Project", "Tower No", "Size (Gross)").
                // We do NOT hard-require specific header text; we map by position.

                int rowNumber = 2; // Start from row 2 (after header)
                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var columns = ParseCsvLine(line);
                    // Expected data columns (after optional ProjectID):
                    // 1) SubProject 2) Tower/Block 3) Size 4) Unit No 5) Floor 6) Type 7) Additional Info 8) Street
                    // Some files may omit trailing optional columns (Additional Info / Street).
                    if (columns.Count >= startIndex + 6)
                    {
                        var subProject = columns.Count > startIndex ? columns[startIndex]?.Trim() ?? string.Empty : string.Empty;
                        var block = columns.Count > dataStartIndex ? columns[dataStartIndex]?.Trim() ?? string.Empty : string.Empty;
                        var sizeRaw = columns.Count > dataStartIndex + 1 ? columns[dataStartIndex + 1]?.Trim() ?? string.Empty : string.Empty;
                        var size = NormalizeNumericToTwoDecimals(sizeRaw);
                        var plotNo = columns.Count > dataStartIndex + 2 ? columns[dataStartIndex + 2]?.Trim() ?? string.Empty : string.Empty;
                        var floor = columns.Count > dataStartIndex + 3 ? columns[dataStartIndex + 3]?.Trim() ?? string.Empty : string.Empty;
                        var plotType = columns.Count > dataStartIndex + 4 ? columns[dataStartIndex + 4]?.Trim() ?? string.Empty : string.Empty;

                        // Optional columns (may be missing or repurposed in some templates).
                        var additionalInfo = columns.Count > dataStartIndex + 5 ? columns[dataStartIndex + 5]?.Trim() ?? string.Empty : string.Empty;
                        var street = columns.Count > dataStartIndex + 6 ? columns[dataStartIndex + 6]?.Trim() ?? string.Empty : string.Empty;

                        if (string.IsNullOrWhiteSpace(block) &&
                            string.IsNullOrWhiteSpace(size) &&
                            string.IsNullOrWhiteSpace(plotNo) &&
                            string.IsNullOrWhiteSpace(floor) &&
                            string.IsNullOrWhiteSpace(plotType) &&
                            string.IsNullOrWhiteSpace(additionalInfo) &&
                            string.IsNullOrWhiteSpace(street))
                        {
                            rowNumber++;
                            continue;
                        }

                        properties.Add(new PropertyImportViewModel
                        {
                            RowNumber = rowNumber,
                            SubProject = subProject,
                            PlotNo = plotNo,
                            Block = block,
                            Size = size,
                            PropertyType = string.Empty,
                            Floor = floor,
                            AdditionalInfo = additionalInfo,
                            Street = street,
                            PlotType = plotType
                        });
                    }
                    rowNumber++;
                }
            }
            return properties;
        }

        private List<string> ParseCsvLine(string line)
        {
            var columns = new List<string>();
            var currentColumn = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == '\t' && !inQuotes) // CSV can be tab-separated
                {
                    columns.Add(currentColumn.ToString());
                    currentColumn.Clear();
                }
                else if (c == ',' && !inQuotes)
                {
                    columns.Add(currentColumn.ToString());
                    currentColumn.Clear();
                }
                else
                {
                    currentColumn.Append(c);
                }
            }
            columns.Add(currentColumn.ToString()); // Add last column

            return columns;
        }

        private List<PropertyImportViewModel> ParseExcelFile(IFormFile file)
        {
            var properties = new List<PropertyImportViewModel>();
            
            try
            {
                using (var stream = file.OpenReadStream())
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.First();
                    var headerValue = GetCellValue(worksheet, 1, 1);
                    var hasProjectColumn = string.Equals(headerValue, "ProjectID", StringComparison.OrdinalIgnoreCase);
                    var startColumn = hasProjectColumn ? 2 : 1;
                    var hasSubProjectColumn = string.Equals(GetCellValue(worksheet, 1, startColumn), "SubProject", StringComparison.OrdinalIgnoreCase);
                    // New format requires SubProject as first data column (after optional ProjectID).
                    var dataStartColumn = startColumn + 1;
                    // Header names can vary (e.g. "Sub Project", "Tower No", "Size (Gross)").
                    // We do NOT hard-require specific header text; we map by position (and tolerate extra columns).
                    var rows = worksheet.RowsUsed().Skip(1); // Skip header row
                    
                    int rowNumber = 2;
                    foreach (var row in rows)
                    {
                        var subProject = GetCellValue(worksheet, row.RowNumber(), startColumn) ?? string.Empty;
                        var block = GetCellValue(worksheet, row.RowNumber(), dataStartColumn) ?? string.Empty;
                        var sizeRaw = GetCellValue(worksheet, row.RowNumber(), dataStartColumn + 1) ?? string.Empty;
                        var size = NormalizeNumericToTwoDecimals(sizeRaw);
                        var plotNo = GetCellValue(worksheet, row.RowNumber(), dataStartColumn + 2) ?? string.Empty;
                        var floor = GetCellValue(worksheet, row.RowNumber(), dataStartColumn + 3) ?? string.Empty;
                        var plotType = GetCellValue(worksheet, row.RowNumber(), dataStartColumn + 4) ?? string.Empty;
                        // Optional columns (may be missing in some templates).
                        var additionalInfo = GetCellValue(worksheet, row.RowNumber(), dataStartColumn + 5) ?? string.Empty;
                        var street = GetCellValue(worksheet, row.RowNumber(), dataStartColumn + 6) ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(block) &&
                            string.IsNullOrWhiteSpace(size) &&
                            string.IsNullOrWhiteSpace(plotNo) &&
                            string.IsNullOrWhiteSpace(floor) &&
                            string.IsNullOrWhiteSpace(plotType) &&
                            string.IsNullOrWhiteSpace(additionalInfo) &&
                            string.IsNullOrWhiteSpace(street))
                        {
                            rowNumber++;
                            continue;
                        }
                        
                        properties.Add(new PropertyImportViewModel
                        {
                            RowNumber = rowNumber,
                            SubProject = subProject,
                            PlotNo = plotNo,
                            Block = block,
                            Size = size,
                            PropertyType = string.Empty,
                            Floor = floor,
                            AdditionalInfo = additionalInfo,
                            Street = street,
                            PlotType = plotType
                        });
                        rowNumber++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading Excel file: {ex.Message}");
            }
            
            return properties;
        }

        private string? GetCellValue(IXLWorksheet worksheet, int row, int column)
        {
            var cell = worksheet.Cell(row, column);
            var value = cell.GetValue<string>();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public async Task<IActionResult> Reserve(string projectId = null)
        {
            var query = _context.Properties
                .Include(p => p.Project)
                .Include(p => p.Dealer)
                .Where(p => p.Status == "Available");

            // Filter by project if selected
            if (!string.IsNullOrEmpty(projectId))
            {
                query = query.Where(p => p.ProjectID == projectId);
            }

            var availableProperties = await query
                .OrderBy(p => p.ProjectID)
                .ThenBy(p => p.PlotNo)
                .ToListAsync();

            ViewBag.Dealers = await _context.Dealers
                .Where(d => d.Status == "Active")
                .OrderBy(d => d.DealershipName)
                .ToListAsync();

            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.SelectedProjectId = projectId;

            return View(availableProperties);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeDealer(List<string> propertyIds, int fromDealerId, int toDealerId, string remarks)
        {
            if (propertyIds == null || propertyIds.Count == 0)
            {
                TempData["Error"] = "Please select at least one property.";
                return RedirectToAction(nameof(Reserve));
            }

            if (toDealerId <= 0)
            {
                TempData["Error"] = "Please select a target dealer.";
                return RedirectToAction(nameof(Reserve));
            }

            // Verify target dealer exists
            var targetDealer = await _context.Dealers.FindAsync(toDealerId);
            if (targetDealer == null)
            {
                TempData["Error"] = "Selected dealer not found.";
                return RedirectToAction(nameof(Reserve));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ADMIN";
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var propertyId in propertyIds)
            {
                try
                {
                    var property = await _context.Properties.FindAsync(propertyId);
                    if (property == null || property.Status != "Available")
                    {
                        skippedCount++;
                        continue;
                    }

                    // Get old dealer name for logging
                    string? oldDealerName = null;
                    if (property.DealerID.HasValue)
                    {
                        var oldDealer = await _context.Dealers.FindAsync(property.DealerID.Value);
                        oldDealerName = oldDealer?.DealershipName;
                    }

                    // Get new dealer name
                    var newDealerName = targetDealer.DealershipName;

                    // Update property
                    property.DealerID = toDealerId;
                    _context.Update(property);

                    // Create log entry
                    var log = new PropertyLog
                    {
                        PropertyID = propertyId,
                        Action = "Dealer Changed",
                        OldValue = oldDealerName ?? $"DealerID: {property.DealerID}",
                        NewValue = newDealerName ?? $"DealerID: {toDealerId}",
                        Remarks = remarks ?? $"Bulk dealer change from {(property.DealerID.HasValue ? oldDealerName : "None")} to {newDealerName}",
                        CreatedAt = DateTime.Now,
                        CreatedBy = userId
                    };

                    _context.PropertyLogs.Add(log);
                    updatedCount++;
                }
                catch (Exception)
                {
                    skippedCount++;
                    continue;
                }
            }

            await _context.SaveChangesAsync();

            // Log activity
            if (userId != null)
            {
                await LogActivity(userId, $"Bulk Dealer Change - {updatedCount} properties updated", "Property", "Bulk");
            }

            TempData["Success"] = $"Successfully updated {updatedCount} properties. {skippedCount} skipped.";
            return RedirectToAction(nameof(Reserve));
        }

        private string GenerateID()
        {
            return Guid.NewGuid().ToString("N")[..10].ToUpper();
        }
    }
}
