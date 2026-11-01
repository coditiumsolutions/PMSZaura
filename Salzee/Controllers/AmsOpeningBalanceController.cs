using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsOpeningBalanceController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsOpeningBalanceController(PMSDbContext context, IModulePermissionService modulePermission)
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
        ViewBag.CanEdit = _modulePermission.CanEdit(perm);
        return null;
    }

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

    [HttpGet]
    public async Task<IActionResult> Index(int? fiscalYearId)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var years = await _context.AccFiscalYears.AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .ToListAsync();
        ViewBag.FiscalYears = years;

        var fyId = fiscalYearId ?? years.FirstOrDefault()?.FiscalYearID ?? 0;
        if (fyId == 0)
        {
            return View(new AmsOpeningBalanceIndexVm { FiscalYearID = 0, Rows = new List<AmsOpeningBalanceRowVm>() });
        }

        var vm = await BuildGridAsync(fyId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AmsOpeningBalanceIndexVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (model.FiscalYearID <= 0 || model.Rows == null || model.Rows.Count == 0)
        {
            TempData["Error"] = "Invalid data.";
            return RedirectToAction(nameof(Index), new { fiscalYearId = model.FiscalYearID });
        }

        foreach (var row in model.Rows)
        {
            if (row.IsPosted)
                continue;

            var debit = row.DebitAmount;
            var credit = row.CreditAmount;
            if (debit < 0 || credit < 0) continue;
            if (debit > 0 && credit > 0)
            {
                TempData["Error"] = "Enter either debit or credit per row, not both.";
                return RedirectToAction(nameof(Index), new { fiscalYearId = model.FiscalYearID });
            }

            if (row.OpeningBalanceID is > 0)
            {
                var ob = await _context.AccOpeningBalances.FirstOrDefaultAsync(o =>
                    o.OpeningBalanceID == row.OpeningBalanceID!.Value && o.FiscalYearID == model.FiscalYearID);
                if (ob == null || ob.IsPosted) continue;

                if (debit == 0 && credit == 0)
                {
                    _context.AccOpeningBalances.Remove(ob);
                }
                else
                {
                    ob.DebitAmount = debit;
                    ob.CreditAmount = credit;
                    ob.Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();
                }
            }
            else if (debit > 0 || credit > 0)
            {
                var dup = await _context.AccOpeningBalances.AnyAsync(o =>
                    o.FiscalYearID == model.FiscalYearID
                    && o.AccountHeadID == row.AccountHeadID
                    && o.SubLedgerType == null
                    && o.SubLedgerID == null);
                if (dup) continue;

                _context.AccOpeningBalances.Add(new AccOpeningBalance
                {
                    FiscalYearID = model.FiscalYearID,
                    AccountHeadID = row.AccountHeadID,
                    SubLedgerType = null,
                    SubLedgerID = null,
                    DebitAmount = debit,
                    CreditAmount = credit,
                    IsPosted = false,
                    Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CurrentUserId
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Opening balances saved.";
        return RedirectToAction(nameof(Index), new { fiscalYearId = model.FiscalYearID });
    }

    private async Task<AmsOpeningBalanceIndexVm> BuildGridAsync(int fiscalYearId)
    {
        var heads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && !h.IsControlAccount && (h.AllowDirectPosting || h.AccountLevel >= 3))
            .OrderBy(h => h.AccountCode)
            .ToListAsync();

        var obs = await _context.AccOpeningBalances.AsNoTracking()
            .Where(o => o.FiscalYearID == fiscalYearId && o.SubLedgerType == null && o.SubLedgerID == null)
            .ToListAsync();
        var byHead = obs
            .GroupBy(o => o.AccountHeadID)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.OpeningBalanceID).First());

        var rows = new List<AmsOpeningBalanceRowVm>();
        foreach (var h in heads)
        {
            byHead.TryGetValue(h.AccountHeadID, out var ob);
            rows.Add(new AmsOpeningBalanceRowVm
            {
                AccountHeadID = h.AccountHeadID,
                AccountCode = h.AccountCode,
                AccountName = h.AccountName,
                OpeningBalanceID = ob?.OpeningBalanceID,
                DebitAmount = ob?.DebitAmount ?? 0,
                CreditAmount = ob?.CreditAmount ?? 0,
                Notes = ob?.Notes,
                IsPosted = ob?.IsPosted ?? false
            });
        }

        return new AmsOpeningBalanceIndexVm { FiscalYearID = fiscalYearId, Rows = rows };
    }
}
