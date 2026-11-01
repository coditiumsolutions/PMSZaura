using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsBudgetController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;
    private readonly AmsAdminDeleteService _adminDelete;

    public AmsBudgetController(
        PMSDbContext context,
        IModulePermissionService modulePermission,
        AmsAdminDeleteService adminDelete)
    {
        _context = context;
        _modulePermission = modulePermission;
        _adminDelete = adminDelete;
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

    private static string DbUserId10(string? userId) =>
        string.IsNullOrEmpty(userId) ? "SYSTEM" : userId.Length <= 10 ? userId : userId[..10];

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

    private async Task SetViewBagEditFlagsAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
        ViewBag.CanCreate = _modulePermission.CanEdit(perm);
        ViewBag.CanEdit = _modulePermission.CanEdit(perm);
    }

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccBudgets.AsNoTracking()
            .Include(b => b.FiscalYear)
            .OrderByDescending(b => b.CreatedAt)
            .Take(200)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.FiscalYears = await _context.AccFiscalYears.AsNoTracking()
            .OrderByDescending(f => f.StartDate)
            .Take(30)
            .ToListAsync();
        return View(new AccBudget { BudgetType = "Annual", Status = "Draft" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccBudget model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.BudgetName = model.BudgetName.Trim();
        if (!ModelState.IsValid)
        {
            ViewBag.FiscalYears = await _context.AccFiscalYears.AsNoTracking()
                .OrderByDescending(f => f.StartDate).Take(30).ToListAsync();
            return View(model);
        }

        model.BudgetID = 0;
        model.CreatedBy = DbUserId10(CurrentUserId);
        model.CreatedAt = DateTime.UtcNow;
        _context.AccBudgets.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Budget created.";
        return RedirectToAction(nameof(Details), new { id = model.BudgetID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var b = await _context.AccBudgets.FirstOrDefaultAsync(x => x.BudgetID == id);
        if (b == null) return NotFound();
        ViewBag.FiscalYears = await _context.AccFiscalYears.AsNoTracking()
            .OrderByDescending(f => f.StartDate).Take(30).ToListAsync();
        return View(b);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccBudget model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.BudgetID) return BadRequest();

        model.BudgetName = model.BudgetName.Trim();
        if (!ModelState.IsValid)
        {
            ViewBag.FiscalYears = await _context.AccFiscalYears.AsNoTracking()
                .OrderByDescending(f => f.StartDate).Take(30).ToListAsync();
            return View(model);
        }

        var b = await _context.AccBudgets.FirstOrDefaultAsync(x => x.BudgetID == id);
        if (b == null) return NotFound();
        b.BudgetName = model.BudgetName;
        b.FiscalYearID = model.FiscalYearID;
        b.BudgetType = model.BudgetType;
        b.Status = model.Status;
        b.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        await _context.SaveChangesAsync();
        TempData["Success"] = "Budget updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;
        await SetViewBagEditFlagsAsync();

        var b = await _context.AccBudgets.AsNoTracking()
            .Include(x => x.FiscalYear)
            .Include(x => x.Lines).ThenInclude(l => l.AccountHead)
            .Include(x => x.Lines).ThenInclude(l => l.Period)
            .Include(x => x.Lines).ThenInclude(l => l.CostCenter)
            .FirstOrDefaultAsync(x => x.BudgetID == id);
        if (b == null) return NotFound();
        return View(b);
    }

    [HttpGet]
    public async Task<IActionResult> AddLine(int budgetId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (!await _context.AccBudgets.AnyAsync(b => b.BudgetID == budgetId)) return NotFound();
        ViewBag.BudgetId = budgetId;
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive).OrderBy(h => h.AccountCode).Take(500).ToListAsync();
        ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
            .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
        ViewBag.CostCenters = await _context.AccCostCenters.AsNoTracking()
            .Where(c => c.IsActive).OrderBy(c => c.CostCenterCode).Take(200).ToListAsync();
        return View(new AccBudgetLine { BudgetID = budgetId, BudgetedAmount = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(int budgetId, AccBudgetLine model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (budgetId != model.BudgetID) return BadRequest();

        if (!await _context.AccBudgets.AnyAsync(b => b.BudgetID == budgetId)) return NotFound();

        model.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();
        if (model.Remarks != null && model.Remarks.Length > 300) model.Remarks = model.Remarks[..300];

        if (!ModelState.IsValid)
        {
            ViewBag.BudgetId = budgetId;
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive).OrderBy(h => h.AccountCode).Take(500).ToListAsync();
            ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
                .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
            ViewBag.CostCenters = await _context.AccCostCenters.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.CostCenterCode).Take(200).ToListAsync();
            return View(model);
        }

        model.BudgetLineID = 0;
        _context.AccBudgetLines.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Budget line added.";
        return RedirectToAction(nameof(Details), new { id = budgetId });
    }

    [HttpGet]
    public async Task<IActionResult> EditLine(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var line = await _context.AccBudgetLines.FirstOrDefaultAsync(l => l.BudgetLineID == id);
        if (line == null) return NotFound();
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive).OrderBy(h => h.AccountCode).Take(500).ToListAsync();
        ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
            .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
        ViewBag.CostCenters = await _context.AccCostCenters.AsNoTracking()
            .Where(c => c.IsActive).OrderBy(c => c.CostCenterCode).Take(200).ToListAsync();
        return View(line);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLine(int id, AccBudgetLine model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.BudgetLineID) return BadRequest();

        model.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();
        if (model.Remarks != null && model.Remarks.Length > 300) model.Remarks = model.Remarks[..300];

        if (!ModelState.IsValid)
        {
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive).OrderBy(h => h.AccountCode).Take(500).ToListAsync();
            ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
                .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
            ViewBag.CostCenters = await _context.AccCostCenters.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.CostCenterCode).Take(200).ToListAsync();
            return View(model);
        }

        var line = await _context.AccBudgetLines.FirstOrDefaultAsync(l => l.BudgetLineID == id);
        if (line == null) return NotFound();
        line.AccountHeadID = model.AccountHeadID;
        line.CostCenterID = model.CostCenterID;
        line.PeriodID = model.PeriodID;
        line.BudgetedAmount = model.BudgetedAmount;
        line.RevisedAmount = model.RevisedAmount;
        line.Remarks = model.Remarks;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Budget line updated.";
        return RedirectToAction(nameof(Details), new { id = line.BudgetID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLine(int id)
    {
        var denied = await EnsurePermissionAsync("Admin");
        if (denied != null) return denied;

        var line = await _context.AccBudgetLines.AsNoTracking()
            .FirstOrDefaultAsync(l => l.BudgetLineID == id);
        if (line == null) return NotFound();
        var bid = line.BudgetID;
        var result = await _adminDelete.DeleteBudgetLineAsync(id);
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = bid });
    }
}
