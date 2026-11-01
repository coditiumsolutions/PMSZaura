using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsLedgerController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsLedgerController(PMSDbContext context, IModulePermissionService modulePermission)
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
        return null;
    }

    public async Task<IActionResult> TrialBalance(DateTime? asOfDate, int? fiscalYearId)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var asOf = (asOfDate ?? DateTime.UtcNow.Date).Date;
        ViewBag.AsOfDate = asOf;
        ViewBag.FiscalYears = await _context.AccFiscalYears.AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .Select(y => new AmsFiscalYearOptionVm(y.FiscalYearID, y.YearName))
            .ToListAsync();
        ViewBag.FiscalYearId = fiscalYearId;

        var voucherKeys = _context.AccVouchers.AsNoTracking()
            .Where(v => v.Status == "Posted" && !v.IsReversed && v.VoucherDate <= asOf)
            .Select(v => new { v.VoucherID, v.FiscalYearID });
        var heads = _context.AccAccountHeads.AsNoTracking()
            .Select(h => new { h.AccountHeadID, h.AccountCode, h.AccountName });

        var baseQuery = from vl in _context.AccVoucherLines.AsNoTracking()
            join v in voucherKeys on vl.VoucherID equals v.VoucherID
            join h in heads on vl.AccountHeadID equals h.AccountHeadID
            select new { vl.AccountHeadID, h.AccountCode, h.AccountName, vl.DebitAmount, vl.CreditAmount, v.FiscalYearID };

        var filtered = fiscalYearId is > 0
            ? baseQuery.Where(x => x.FiscalYearID == fiscalYearId)
            : baseQuery;

        // Anonymous projection — EF cannot translate GroupBy → AmsTbRowVm constructor with Sum inside.
        var aggregated = await filtered
            .GroupBy(x => new { x.AccountHeadID, x.AccountCode, x.AccountName })
            .Select(g => new
            {
                g.Key.AccountHeadID,
                g.Key.AccountCode,
                g.Key.AccountName,
                TotalDebit = g.Sum(x => x.DebitAmount),
                TotalCredit = g.Sum(x => x.CreditAmount)
            })
            .OrderBy(x => x.AccountCode)
            .ToListAsync();

        var rows = aggregated
            .Select(x => new AmsTbRowVm(x.AccountHeadID, x.AccountCode, x.AccountName, x.TotalDebit, x.TotalCredit))
            .ToList();

        return View(rows);
    }

    public async Task<IActionResult> GeneralLedger(int? accountHeadId, DateTime? fromDate, DateTime? toDate)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var from = (fromDate ?? DateTime.UtcNow.Date.AddMonths(-1)).Date;
        var to = (toDate ?? DateTime.UtcNow.Date).Date;
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
        ViewBag.AccountHeadId = accountHeadId;

        if (accountHeadId is null || accountHeadId.Value <= 0)
            return View(new List<AmsGlRowVm>());

        var aid = accountHeadId!.Value;
        var vouchersInRange = _context.AccVouchers.AsNoTracking()
            .Where(v => v.Status == "Posted" && !v.IsReversed && v.VoucherDate >= from && v.VoucherDate <= to)
            .Select(v => new { v.VoucherID, v.VoucherDate, v.VoucherNo, v.VoucherTypeID });

        var rows = await _context.AccVoucherLines.AsNoTracking()
            .Where(vl => vl.AccountHeadID == aid)
            .Join(vouchersInRange, vl => vl.VoucherID, v => v.VoucherID, (vl, v) => new { vl, v })
            .Join(_context.AccVoucherTypes.AsNoTracking(), x => x.v.VoucherTypeID, vt => vt.VoucherTypeID, (x, vt) => new { x.vl, x.v, vt })
            .OrderBy(x => x.v.VoucherDate).ThenBy(x => x.v.VoucherID).ThenBy(x => x.vl.LineNumber)
            .Select(x => new AmsGlRowVm(
                x.v.VoucherDate,
                x.v.VoucherNo,
                x.vt.TypeCode,
                x.vl.LineNumber,
                x.vl.Description,
                x.vl.DebitAmount,
                x.vl.CreditAmount))
            .ToListAsync();

        return View(rows);
    }

    /// <summary>Recent vouchers with type (read-only register).</summary>
    public async Task<IActionResult> PostedVouchers(int? voucherTypeId, DateTime? fromDate, DateTime? toDate)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var from = (fromDate ?? DateTime.UtcNow.Date.AddMonths(-3)).Date;
        var to = (toDate ?? DateTime.UtcNow.Date).Date;
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.VoucherTypeId = voucherTypeId;
        ViewBag.VoucherTypes = await _context.AccVoucherTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.TypeCode)
            .ToListAsync();

        var filterTypeId = voucherTypeId ?? 0;

        var query =
            _context.AccVouchers.AsNoTracking()
                .Join(
                    _context.AccVoucherTypes.AsNoTracking(),
                    v => v.VoucherTypeID,
                    vt => vt.VoucherTypeID,
                    (v, vt) => new { v, vt })
                .Where(x =>
                    x.v.VoucherDate >= from &&
                    x.v.VoucherDate <= to &&
                    (filterTypeId <= 0 || x.v.VoucherTypeID == filterTypeId))
                .OrderByDescending(x => x.v.VoucherDate)
                .ThenByDescending(x => x.v.VoucherID)
                .Select(x => new AmsPostedVoucherRowVm(
                    x.v.VoucherID,
                    x.v.VoucherDate,
                    x.v.VoucherNo,
                    x.vt.TypeCode,
                    x.vt.TypeName,
                    x.v.Status,
                    x.v.TotalDebit,
                    x.v.TotalCredit));

        var rows = await query.Take(500).ToListAsync();
        return View(rows);
    }
}
