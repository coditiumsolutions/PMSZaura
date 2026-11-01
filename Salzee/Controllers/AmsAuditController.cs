using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsAuditController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private const int PageSize = 50;

    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsAuditController(PMSDbContext context, IModulePermissionService modulePermission)
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
        if (requiredLevel == "Admin" && !await HttpContext.RequestServices.GetRequiredService<AmsAccessService>().IsAdminUserAsync(userId))
            return RedirectToAction("AccessDenied", "Account");
        ViewBag.CanCreate = _modulePermission.CanEdit(perm);
        ViewBag.CanEdit = _modulePermission.CanEdit(perm);
        return null;
    }

    public async Task<IActionResult> Index(int page = 1, string? table = null, string? action = null)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        page = page < 1 ? 1 : page;
        var q = _context.AccAccountingAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(table))
        {
            var t = table.Trim();
            q = q.Where(x => x.TableName.Contains(t));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var a = action.Trim().ToUpperInvariant();
            q = q.Where(x => x.Action == a);
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(x => x.LogID)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.Total = total;
        ViewBag.PageSize = PageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.FilterTable = table ?? "";
        ViewBag.FilterAction = action ?? "";

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var row = await _context.AccAccountingAuditLogs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LogID == id);
        if (row == null)
            return NotFound();

        return View(row);
    }
}
