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
    public class SettingsController : Controller
    {
        private const string ModuleKey = "Settings";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private readonly ISiteConfigService _siteConfigService;

        public SettingsController(PMSDbContext context, IModulePermissionService modulePermission, ISiteConfigService siteConfigService)
        {
            _context = context;
            _modulePermission = modulePermission;
            _siteConfigService = siteConfigService;
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

            // Load every Configuration row (no filters / joins that could drop records).
            var configurations = await _context.Configurations
                .AsNoTracking()
                .OrderBy(c => c.Category ?? string.Empty)
                .ThenBy(c => c.ConfigKey)
                .ToListAsync();

            return View(configurations);
        }

        [HttpGet]
        public async Task<IActionResult> SiteConfig()
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            var model = await _siteConfigService.GetAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiteConfig(SiteConfig model, IFormFile? logo)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (logo != null && logo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "branding");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"logo-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(logo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logo.CopyToAsync(stream);
                }

                model.LogoPath = $"~/images/branding/{fileName}";
            }

            await _siteConfigService.SaveAsync(model);
            TempData["Success"] = "Site configuration updated successfully.";
            return RedirectToAction(nameof(SiteConfig));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            return View(new Configuration { Category = "General" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Configuration configuration)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (ModelState.IsValid)
            {
                // Check if key already exists
                var existing = await _context.Configurations.FindAsync(configuration.ConfigKey);
                if (existing != null)
                {
                    ViewBag.Error = "Configuration key already exists.";
                    return View(configuration);
                }

                configuration.CreatedAt = DateTime.Now;
                configuration.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                configuration.UpdatedAt = DateTime.Now;

                _context.Configurations.Add(configuration);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Configuration added successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(configuration);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var configuration = await _context.Configurations.FindAsync(id);
            if (configuration == null)
            {
                return NotFound();
            }

            return View(configuration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Configuration configuration)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(configuration.ConfigKey))
                return NotFound();

            var existing = await _context.Configurations
                .FirstOrDefaultAsync(c => c.ConfigKey == configuration.ConfigKey);
            if (existing == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existing.Category = configuration.Category;
                    existing.ConfigValue = configuration.ConfigValue;
                    existing.Description = configuration.Description;
                    existing.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    existing.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Configuration updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ViewBag.Error = "An error occurred while updating the configuration.";
                }
            }

            return View(configuration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            var configuration = await _context.Configurations.FindAsync(id);
            if (configuration != null)
            {
                _context.Configurations.Remove(configuration);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Configuration deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var configs = await _context.Configurations
                .Where(c => c.Category == category)
                .Select(c => c.ConfigValue)
                .ToListAsync();

            return Json(configs);
        }
    }
}

