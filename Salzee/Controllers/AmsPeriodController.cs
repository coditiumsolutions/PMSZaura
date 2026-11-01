using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsPeriodController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsPeriodController(PMSDbContext context, IModulePermissionService modulePermission)
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

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var years = await _context.AccFiscalYears.AsNoTracking()
            .Include(y => y.Periods)
            .OrderByDescending(y => y.StartDate)
            .ToListAsync();
        return View(years);
    }

    [HttpGet]
    public async Task<IActionResult> CreateYear()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        return View(new AccFiscalYear { Status = "Open" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateYear([Bind(nameof(AccFiscalYear.YearName), nameof(AccFiscalYear.StartDate), nameof(AccFiscalYear.EndDate), nameof(AccFiscalYear.Status))] AccFiscalYear model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (model.EndDate < model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be on or after start date.");

        if (!ModelState.IsValid)
            return View(model);

        model.CreatedAt = DateTime.UtcNow;
        model.CreatedBy = CurrentUserId;
        _context.AccFiscalYears.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Fiscal year created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditYear(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var fy = await _context.AccFiscalYears.FindAsync(id);
        if (fy == null) return NotFound();
        return View(fy);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditYear(int id, [Bind(nameof(AccFiscalYear.FiscalYearID), nameof(AccFiscalYear.YearName), nameof(AccFiscalYear.StartDate), nameof(AccFiscalYear.EndDate), nameof(AccFiscalYear.Status))] AccFiscalYear model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (id != model.FiscalYearID) return BadRequest();
        var fy = await _context.AccFiscalYears.FindAsync(id);
        if (fy == null) return NotFound();

        if (model.EndDate < model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be on or after start date.");

        if (!ModelState.IsValid)
            return View(model);

        fy.YearName = model.YearName;
        fy.StartDate = model.StartDate;
        fy.EndDate = model.EndDate;
        fy.Status = model.Status;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Fiscal year updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Periods(int id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var fy = await _context.AccFiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FiscalYearID == id);
        if (fy == null) return NotFound();

        var periods = await _context.AccAccountingPeriods.AsNoTracking()
            .Where(p => p.FiscalYearID == id)
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        ViewBag.FiscalYear = fy;
        return View(periods);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateMonths(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var fy = await _context.AccFiscalYears.FirstOrDefaultAsync(f => f.FiscalYearID == id);
        if (fy == null) return NotFound();

        var added = 0;
        for (var monthStart = new DateTime(fy.StartDate.Year, fy.StartDate.Month, 1);
             monthStart <= fy.EndDate;
             monthStart = monthStart.AddMonths(1))
        {
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var clipStart = monthStart < fy.StartDate ? fy.StartDate : monthStart;
            var clipEnd = monthEnd > fy.EndDate ? fy.EndDate : monthEnd;
            if (clipStart > clipEnd) continue;

            var name = clipStart.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var exists = await _context.AccAccountingPeriods.AnyAsync(p =>
                p.FiscalYearID == id && p.StartDate == clipStart);
            if (exists) continue;

            _context.AccAccountingPeriods.Add(new AccAccountingPeriod
            {
                FiscalYearID = id,
                PeriodName = name,
                StartDate = clipStart,
                EndDate = clipEnd,
                Status = "Open"
            });
            added++;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Generated {added} period(s).";
        return RedirectToAction(nameof(Periods), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClosePeriod(int id, int fiscalYearId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var period = await _context.AccAccountingPeriods.FirstOrDefaultAsync(p => p.PeriodID == id);
        if (period == null || period.FiscalYearID != fiscalYearId) return NotFound();

        var blocking = await CountNonTerminalVouchersInPeriodAsync(id);
        if (blocking > 0)
        {
            TempData["Error"] = $"Cannot close: {blocking} voucher(s) are not Posted, Cancelled, or Reversed (plan §6).";
            return RedirectToAction(nameof(Periods), new { id = fiscalYearId });
        }

        period.Status = "Closed";
        period.ClosedBy = CurrentUserId;
        period.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Period closed.";
        return RedirectToAction(nameof(Periods), new { id = fiscalYearId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenPeriod(int id, int fiscalYearId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var period = await _context.AccAccountingPeriods.FirstOrDefaultAsync(p => p.PeriodID == id);
        if (period == null || period.FiscalYearID != fiscalYearId) return NotFound();

        var fy = await _context.AccFiscalYears.AsNoTracking().FirstOrDefaultAsync(f => f.FiscalYearID == fiscalYearId);
        if (fy != null && (fy.Status == "Closed" || fy.Status == "Locked"))
        {
            TempData["Error"] = "Reopen the fiscal year first (year is closed or locked).";
            return RedirectToAction(nameof(Periods), new { id = fiscalYearId });
        }

        period.Status = "Open";
        period.ClosedBy = null;
        period.ClosedAt = null;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Period reopened.";
        return RedirectToAction(nameof(Periods), new { id = fiscalYearId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseYear(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var fy = await _context.AccFiscalYears.Include(f => f.Periods)
            .FirstOrDefaultAsync(f => f.FiscalYearID == id);
        if (fy == null) return NotFound();

        if (fy.Periods.Any(p => p.Status == "Open"))
        {
            TempData["Error"] = "Close all accounting periods before closing the fiscal year.";
            return RedirectToAction(nameof(Index));
        }

        fy.Status = "Closed";
        fy.ClosedBy = CurrentUserId;
        fy.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Fiscal year closed.";
        return RedirectToAction(nameof(Index));
    }

    private Task<int> CountNonTerminalVouchersInPeriodAsync(int periodId) =>
        _context.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value] FROM acc.Voucher
                WHERE PeriodID = {periodId}
                AND Status NOT IN ('Posted', 'Cancelled', 'Reversed')
                """)
            .SingleAsync();
}
