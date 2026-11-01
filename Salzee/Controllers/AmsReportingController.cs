using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

/// <summary>AMS analytical reports (budget vs actual, cash-flow proxy, project P&amp;L).</summary>
[Authorize]
public class AmsReportingController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsReportingController(PMSDbContext context, IModulePermissionService modulePermission)
    {
        _context = context;
        _modulePermission = modulePermission;
    }

    private async Task<IActionResult?> EnsureReadAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
        if (!_modulePermission.CanRead(perm))
            return RedirectToAction("AccessDenied", "Account");
        return null;
    }

    public async Task<IActionResult> BudgetVsActual(int? budgetId)
    {
        var denied = await EnsureReadAsync();
        if (denied != null) return denied;

        ViewBag.Budgets = await _context.AccBudgets.AsNoTracking()
            .Include(b => b.FiscalYear)
            .OrderByDescending(b => b.CreatedAt)
            .Take(100)
            .ToListAsync();

        if (budgetId is null or <= 0)
            return View("BudgetVsActual", new List<AmsBudgetVsActualRowVm>());

        var lines = await _context.AccBudgetLines.AsNoTracking()
            .Where(l => l.BudgetID == budgetId)
            .Include(l => l.AccountHead)
            .Include(l => l.Period)
            .OrderBy(l => l.AccountHead!.AccountCode)
            .ToListAsync();

        ViewBag.SelectedBudgetId = budgetId;
        var rows = new List<AmsBudgetVsActualRowVm>();
        foreach (var line in lines)
        {
            var ahId = line.AccountHeadID;
            var actual = await (
                from vl in _context.AccVoucherLines.AsNoTracking()
                join v in _context.AccVouchers.AsNoTracking() on vl.VoucherID equals v.VoucherID
                where v.Status == "Posted" && vl.AccountHeadID == ahId
                select vl.DebitAmount - vl.CreditAmount
            ).SumAsync();

            var budgetAmt = line.RevisedAmount ?? line.BudgetedAmount;
            rows.Add(new AmsBudgetVsActualRowVm
            {
                BudgetLineID = line.BudgetLineID,
                AccountHeadID = ahId,
                AccountCode = line.AccountHead?.AccountCode ?? "",
                AccountName = line.AccountHead?.AccountName ?? "",
                CostCenterID = line.CostCenterID,
                PeriodID = line.PeriodID,
                PeriodName = line.Period?.PeriodName,
                BudgetAmount = budgetAmt,
                ActualAmount = Math.Round(actual, 2, MidpointRounding.AwayFromZero)
            });
        }

        return View("BudgetVsActual", rows);
    }

    /// <summary>Posted voucher totals by calendar month (proxy; not a full indirect cash flow statement).</summary>
    public async Task<IActionResult> CashFlow(int? fiscalYearId)
    {
        var denied = await EnsureReadAsync();
        if (denied != null) return denied;

        ViewBag.FiscalYears = await _context.AccFiscalYears.AsNoTracking()
            .OrderByDescending(f => f.StartDate).Take(20).ToListAsync();

        if (fiscalYearId is null or <= 0)
            return View("CashFlow", new List<AmsCashFlowMonthVm>());

        ViewBag.SelectedFiscalYearId = fiscalYearId;
        var fy = await _context.AccFiscalYears.AsNoTracking().FirstOrDefaultAsync(f => f.FiscalYearID == fiscalYearId);
        if (fy == null) return View("CashFlow", new List<AmsCashFlowMonthVm>());

        var vouchers = await _context.AccVouchers.AsNoTracking()
            .Where(v => v.FiscalYearID == fiscalYearId && v.Status == "Posted")
            .Select(v => new { v.VoucherDate, v.TotalDebit, v.TotalCredit })
            .ToListAsync();

        var rows = vouchers
            .GroupBy(v => new { y = v.VoucherDate.Year, m = v.VoucherDate.Month })
            .OrderBy(g => g.Key.y).ThenBy(g => g.Key.m)
            .Select(g => new AmsCashFlowMonthVm
            {
                Year = g.Key.y,
                Month = g.Key.m,
                PostedDebits = g.Sum(x => x.TotalDebit),
                PostedCredits = g.Sum(x => x.TotalCredit)
            })
            .ToList();

        ViewBag.FiscalYearName = fy.YearName;
        return View("CashFlow", rows);
    }

    public async Task<IActionResult> ProjectProfitLoss(string? projectId)
    {
        var denied = await EnsureReadAsync();
        if (denied != null) return denied;

        if (string.IsNullOrWhiteSpace(projectId))
            return View("ProjectProfitLoss", new List<AmsProjectPlRowVm>());

        var pid = projectId.Trim();
        if (pid.Length > 10) pid = pid[..10];

        var lines = await _context.AccVoucherLines.AsNoTracking()
            .Include(vl => vl.AccountHead)!.ThenInclude(h => h!.Category)
            .Include(vl => vl.Voucher)
            .Where(vl => vl.Voucher != null
                         && vl.Voucher.Status == "Posted"
                         && vl.Voucher.PMSProjectID == pid)
            .ToListAsync();

        var rows = lines
            .GroupBy(vl => vl.AccountHead?.Category?.CategoryName ?? "(Uncategorized)")
            .OrderBy(g => g.Key)
            .Select(g => new AmsProjectPlRowVm
            {
                CategoryName = g.Key,
                NetAmount = Math.Round(g.Sum(vl => vl.DebitAmount - vl.CreditAmount), 2, MidpointRounding.AwayFromZero)
            })
            .ToList();

        ViewBag.ProjectId = pid;
        return View("ProjectProfitLoss", rows);
    }
}
