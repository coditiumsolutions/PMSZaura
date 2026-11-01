using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;
using System.Security.Claims;

namespace Salzee.Controllers
{
    [Authorize]
    public class DealerController : Controller
    {
        private const string ModuleKey = "Dealer";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public DealerController(PMSDbContext context, IModulePermissionService modulePermission)
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
            var dealers = await _context.Dealers
                .Include(d => d.Customers)
                .Include(d => d.Properties)
                .ToListAsync();
            return View(dealers);
        }

        public async Task<IActionResult> Report()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var dealers = await _context.Dealers
                .Include(d => d.Customers)
                .Include(d => d.Properties)
                .ToListAsync();
            return View(dealers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers
                .Include(d => d.Customers)
                .Include(d => d.Properties)
                .FirstOrDefaultAsync(d => d.DealerID == id);

            if (dealer == null)
            {
                return NotFound();
            }

            return View(dealer);
        }

        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Dealer dealer)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (ModelState.IsValid)
            {
                _context.Dealers.Add(dealer);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivity(userId, "Create Dealer", "Dealer", dealer.DealerID.ToString());
                }

                return RedirectToAction(nameof(Index));
            }

            return View(dealer);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers.FindAsync(id);
            if (dealer == null)
            {
                return NotFound();
            }

            return View(dealer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Dealer dealer)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (id != dealer.DealerID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dealer);
                    await _context.SaveChangesAsync();

                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await LogActivity(userId, "Update Dealer", "Dealer", dealer.DealerID.ToString());
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DealerExists(dealer.DealerID))
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

            return View(dealer);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers
                .Include(d => d.Customers)
                .Include(d => d.Properties)
                .FirstOrDefaultAsync(d => d.DealerID == id);

            if (dealer == null)
            {
                return NotFound();
            }

            return View(dealer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            var dealer = await _context.Dealers.FindAsync(id);
            if (dealer != null)
            {
                _context.Dealers.Remove(dealer);
                await _context.SaveChangesAsync();

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogActivity(userId, "Delete Dealer", "Dealer", id.ToString());
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DealerExists(int id)
        {
            return _context.Dealers.Any(e => e.DealerID == id);
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
    }
}

