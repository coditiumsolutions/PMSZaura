using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;
using Salzee.Utilities;

namespace Salzee.Controllers;

[Authorize]
public class AmsJvController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private const string JvTypeCode = "JV";
    private const string RvTypeCode = "RV";

    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;
    private readonly AmsAdminDeleteService _adminDelete;

    public AmsJvController(
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

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

    private async Task<int> GetJvTypeIdAsync()
    {
        return await _context.AccVoucherTypes.AsNoTracking()
            .Where(t => t.TypeCode == JvTypeCode && t.IsActive)
            .Select(t => t.VoucherTypeID)
            .FirstOrDefaultAsync();
    }

    private async Task<int> GetRvTypeIdAsync()
    {
        return await _context.AccVoucherTypes.AsNoTracking()
            .Where(t => t.TypeCode == RvTypeCode && t.IsActive)
            .Select(t => t.VoucherTypeID)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> TrySaveVoucherHeaderAsync(AccVoucher v, string typeCode, Func<Task> reloadLookupsAsync)
    {
        if (await AmsVoucherNumberHelper.ExistsAsync(_context, v.VoucherNo))
        {
            ViewBag.DuplicateVoucherNo = v.VoucherNo;
            ViewBag.NextVoucherNo = await AmsVoucherNumberHelper.PeekNextAsync(_context, typeCode);
            ModelState.AddModelError(string.Empty, AmsVoucherNumberHelper.DuplicateMessage(v.VoucherNo));
            await reloadLookupsAsync();
            return false;
        }

        _context.AccVouchers.Add(v);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (AmsVoucherNumberHelper.IsUniqueVoucherNoViolation(ex))
        {
            _context.Entry(v).State = EntityState.Detached;
            ViewBag.DuplicateVoucherNo = v.VoucherNo;
            ViewBag.NextVoucherNo = await AmsVoucherNumberHelper.PeekNextAsync(_context, typeCode);
            ModelState.AddModelError(string.Empty, AmsVoucherNumberHelper.DuplicateMessage(v.VoucherNo));
            await reloadLookupsAsync();
            return false;
        }
    }

    private async Task<(bool Ok, string? Error, AccAccountingPeriod? Period)> ValidatePeriodAndDateAsync(
        int periodId, DateTime voucherDate)
    {
        var period = await _context.AccAccountingPeriods.AsNoTracking()
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.PeriodID == periodId);
        if (period?.FiscalYear == null)
            return (false, "Period not found.", null);
        if (!string.Equals(period.Status, "Open", StringComparison.OrdinalIgnoreCase))
            return (false, "Accounting period is not open.", period);
        if (!string.Equals(period.FiscalYear.Status, "Open", StringComparison.OrdinalIgnoreCase))
            return (false, "Fiscal year is not open.", period);
        var d = voucherDate.Date;
        if (d < period.StartDate.Date || d > period.EndDate.Date)
            return (false, "Voucher date must fall within the selected period.", period);
        return (true, null, period);
    }

    private static List<AmsJvLineInputVm> NormalizeLines(IEnumerable<AmsJvLineInputVm> lines)
    {
        return lines
            .Where(l => l.AccountHeadID > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0))
            .ToList();
    }

    private static (bool Ok, string? Error) ValidateLines(IReadOnlyList<AmsJvLineInputVm> lines, bool requireBalanced)
    {
        if (lines.Count < 2)
            return (false, "Enter at least two lines with an account and a debit or credit amount.");
        foreach (var l in lines)
        {
            if (l.DebitAmount > 0 && l.CreditAmount > 0)
                return (false, "Each line may have only debit or only credit, not both.");
            if (l.DebitAmount < 0 || l.CreditAmount < 0)
                return (false, "Amounts cannot be negative.");
        }

        if (!requireBalanced)
            return (true, null);
        var td = lines.Sum(x => x.DebitAmount);
        var tc = lines.Sum(x => x.CreditAmount);
        return td == tc ? (true, null) : (false, $"Voucher is not balanced (debit {td:N2} ≠ credit {tc:N2}).");
    }

    private static (decimal Dr, decimal Cr) Totals(IEnumerable<AmsJvLineInputVm> lines) =>
        (lines.Sum(x => x.DebitAmount), lines.Sum(x => x.CreditAmount));

    private async Task LoadJvLookupsAsync()
    {
        ViewBag.OpenFiscalYears = await AmsAccountingPeriodGuard.GetOpenFiscalYearsAsync(_context);
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
    }

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        if (jvTypeId == 0)
        {
            ViewBag.Error = "Journal voucher type (JV) is missing from acc.VoucherType. Run AMS_Create_acc_schema.sql seeds.";
            return View(new List<AccVoucher>());
        }

        var list = await _context.AccVouchers.AsNoTracking()
            .Where(v => v.VoucherTypeID == jvTypeId)
            .Include(v => v.Period)
            .Include(v => v.FiscalYear)
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.VoucherID)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var v = await _context.AccVouchers.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.AccountHead)
            .Include(x => x.VoucherType)
            .Include(x => x.Period)
            .Include(x => x.FiscalYear)
            .Include(x => x.ReversalOf)
            .FirstOrDefaultAsync(x => x.VoucherID == id);
        if (v?.VoucherType == null) return NotFound();
        if (v.VoucherType.TypeCode != JvTypeCode && v.VoucherType.TypeCode != RvTypeCode)
            return NotFound();
        return View(v);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (await GetJvTypeIdAsync() == 0)
        {
            TempData["Error"] = "JV voucher type missing. Run AMS schema seeds.";
            return RedirectToAction(nameof(Index));
        }

        await LoadJvLookupsAsync();
        ViewBag.NextVoucherNo = await AmsVoucherNumberHelper.PeekNextAsync(_context, JvTypeCode);
        var vm = new AmsJvEditVm { VoucherDate = DateTime.UtcNow.Date, Lines = Enumerable.Range(0, 8).Select(_ => new AmsJvLineInputVm()).ToList() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AmsJvEditVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        if (jvTypeId == 0)
        {
            TempData["Error"] = "JV voucher type missing.";
            return RedirectToAction(nameof(Index));
        }

        var lines = NormalizeLines(model.Lines);
        var (okLines, errLines) = ValidateLines(lines, requireBalanced: true);
        if (!okLines)
            ModelState.AddModelError(string.Empty, errLines!);

        var (okPd, errPd, period) = await AmsAccountingPeriodGuard.ValidateFiscalYearAndDateAsync(
            _context, model.FiscalYearID, model.VoucherDate);
        if (!okPd)
            ModelState.AddModelError(nameof(model.FiscalYearID), errPd!);

        if (!ModelState.IsValid)
        {
            await LoadJvLookupsAsync();
            ViewBag.NextVoucherNo = await AmsVoucherNumberHelper.PeekNextAsync(_context, JvTypeCode);
            while (model.Lines.Count < 8)
                model.Lines.Add(new AmsJvLineInputVm());
            return View(model);
        }

        var (td, tc) = Totals(lines);
        var voucherNo = await AmsVoucherNumberHelper.AllocateNextAsync(_context, JvTypeCode);

        var v = new AccVoucher
        {
            VoucherTypeID = jvTypeId,
            VoucherNo = voucherNo,
            VoucherDate = model.VoucherDate.Date,
            PeriodID = period!.PeriodID,
            FiscalYearID = period.FiscalYearID,
            ReferenceNo = model.ReferenceNo,
            Narration = model.Narration,
            TotalDebit = td,
            TotalCredit = tc,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId
        };
        if (!await TrySaveVoucherHeaderAsync(v, JvTypeCode, LoadJvLookupsAsync))
        {
            while (model.Lines.Count < 8)
                model.Lines.Add(new AmsJvLineInputVm());
            return View(model);
        }

        short n = 1;
        foreach (var l in lines)
        {
            _context.AccVoucherLines.Add(new AccVoucherLine
            {
                VoucherID = v.VoucherID,
                LineNumber = n++,
                AccountHeadID = l.AccountHeadID,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description,
                Currency = "PKR",
                ExchangeRate = 1
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Journal voucher {voucherNo} saved as Draft.";
        return RedirectToAction(nameof(Details), new { id = v.VoucherID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();
        if (!string.Equals(v.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only draft vouchers can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await LoadJvLookupsAsync();
        var vm = new AmsJvEditVm
        {
            VoucherID = v.VoucherID,
            FiscalYearID = v.FiscalYearID,
            VoucherDate = v.VoucherDate,
            ReferenceNo = v.ReferenceNo,
            Narration = v.Narration,
            Lines = v.Lines.OrderBy(l => l.LineNumber).Select(l => new AmsJvLineInputVm
            {
                AccountHeadID = l.AccountHeadID,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description
            }).ToList()
        };
        while (vm.Lines.Count < 8)
            vm.Lines.Add(new AmsJvLineInputVm());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AmsJvEditVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (id != model.VoucherID) return BadRequest();

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();
        if (!string.Equals(v.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only draft vouchers can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var lines = NormalizeLines(model.Lines);
        var (okLines, errLines) = ValidateLines(lines, requireBalanced: true);
        if (!okLines)
            ModelState.AddModelError(string.Empty, errLines!);

        var (okPd, errPd, period) = await AmsAccountingPeriodGuard.ValidateFiscalYearAndDateAsync(
            _context, model.FiscalYearID, model.VoucherDate);
        if (!okPd)
            ModelState.AddModelError(nameof(model.FiscalYearID), errPd!);

        if (!ModelState.IsValid)
        {
            await LoadJvLookupsAsync();
            model.VoucherID = id;
            return View(model);
        }

        var (td, tc) = Totals(lines);
        v.PeriodID = period!.PeriodID;
        v.FiscalYearID = period.FiscalYearID;
        v.VoucherDate = model.VoucherDate.Date;
        v.ReferenceNo = model.ReferenceNo;
        v.Narration = model.Narration;
        v.TotalDebit = td;
        v.TotalCredit = tc;
        _context.AccVoucherLines.RemoveRange(v.Lines);
        short n = 1;
        foreach (var l in lines)
        {
            _context.AccVoucherLines.Add(new AccVoucherLine
            {
                VoucherID = v.VoucherID,
                LineNumber = n++,
                AccountHeadID = l.AccountHeadID,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description,
                Currency = "PKR",
                ExchangeRate = 1
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Journal voucher updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var denied = await EnsurePermissionAsync("Admin");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();

        var result = await _adminDelete.DeleteVoucherAsync(id);
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;
        return RedirectToAction(result.Success ? nameof(Index) : nameof(Details), result.Success ? null : new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();
        if (!string.Equals(v.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only draft vouchers can be submitted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var lines = v.Lines.OrderBy(l => l.LineNumber)
            .Select(l => new AmsJvLineInputVm
            {
                AccountHeadID = l.AccountHeadID,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description
            }).ToList();
        var (ok, err) = ValidateLines(lines, requireBalanced: true);
        if (!ok)
        {
            TempData["Error"] = err;
            return RedirectToAction(nameof(Details), new { id });
        }

        var (okPd, errPd, _) = await ValidatePeriodAndDateAsync(v.PeriodID, v.VoucherDate);
        if (!okPd)
        {
            TempData["Error"] = errPd;
            return RedirectToAction(nameof(Details), new { id });
        }

        v.Status = "Pending";
        v.SubmittedBy = CurrentUserId;
        v.SubmittedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Voucher submitted for approval.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();
        if (!string.Equals(v.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only pending vouchers can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        v.Status = "Approved";
        v.ApprovedBy = CurrentUserId;
        v.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Voucher approved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();
        if (!string.Equals(v.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only approved vouchers can be posted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var lines = v.Lines.OrderBy(l => l.LineNumber)
            .Select(l => new AmsJvLineInputVm
            {
                AccountHeadID = l.AccountHeadID,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description
            }).ToList();
        var (ok, err) = ValidateLines(lines, requireBalanced: true);
        if (!ok)
        {
            TempData["Error"] = err;
            return RedirectToAction(nameof(Details), new { id });
        }

        var (okPd, errPd, _) = await ValidatePeriodAndDateAsync(v.PeriodID, v.VoucherDate);
        if (!okPd)
        {
            TempData["Error"] = errPd;
            return RedirectToAction(nameof(Details), new { id });
        }

        v.TotalDebit = lines.Sum(x => x.DebitAmount);
        v.TotalCredit = lines.Sum(x => x.CreditAmount);
        v.Status = "Posted";
        v.PostedBy = CurrentUserId;
        v.PostedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Voucher posted to the ledger.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Reverse(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var v = await _context.AccVouchers.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.AccountHead)
            .Include(x => x.VoucherType)
            .FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
        if (v == null) return NotFound();
        if (!string.Equals(v.Status, "Posted", StringComparison.OrdinalIgnoreCase) || v.IsReversed)
        {
            TempData["Error"] = "Only posted, non-reversed journal vouchers can be reversed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(v);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseConfirm(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var jvTypeId = await GetJvTypeIdAsync();
        var rvTypeId = await GetRvTypeIdAsync();
        if (rvTypeId == 0)
        {
            TempData["Error"] = "Reversal voucher type (RV) missing from acc.VoucherType.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var orig = await _context.AccVouchers.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.VoucherID == id && x.VoucherTypeID == jvTypeId);
            if (orig == null) return NotFound();
            if (!string.Equals(orig.Status, "Posted", StringComparison.OrdinalIgnoreCase) || orig.IsReversed)
            {
                TempData["Error"] = "Only posted, non-reversed journal vouchers can be reversed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var (okPd, errPd, period) = await ValidatePeriodAndDateAsync(orig.PeriodID, orig.VoucherDate);
            if (!okPd)
            {
                TempData["Error"] = errPd;
                return RedirectToAction(nameof(Details), new { id });
            }

            var voucherNo = await AmsVoucherNumberHelper.AllocateNextAsync(_context, RvTypeCode);
            if (await AmsVoucherNumberHelper.ExistsAsync(_context, voucherNo))
            {
                TempData["Error"] = AmsVoucherNumberHelper.DuplicateMessage(voucherNo);
                return RedirectToAction(nameof(Details), new { id });
            }
            var lines = orig.Lines.OrderBy(l => l.LineNumber).ToList();
            var rev = new AccVoucher
            {
                VoucherTypeID = rvTypeId,
                VoucherNo = voucherNo,
                VoucherDate = orig.VoucherDate,
                PeriodID = orig.PeriodID,
                FiscalYearID = orig.FiscalYearID,
                ReferenceNo = orig.ReferenceNo,
                Narration = $"Reversal of {orig.VoucherNo}",
                TotalDebit = orig.TotalCredit,
                TotalCredit = orig.TotalDebit,
                Status = "Posted",
                ReversalVoucherID = orig.VoucherID,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId,
                PostedBy = CurrentUserId,
                PostedAt = DateTime.UtcNow
            };
            _context.AccVouchers.Add(rev);
            await _context.SaveChangesAsync();

            short n = 1;
            foreach (var l in lines)
            {
                _context.AccVoucherLines.Add(new AccVoucherLine
                {
                    VoucherID = rev.VoucherID,
                    LineNumber = n++,
                    AccountHeadID = l.AccountHeadID,
                    DebitAmount = l.CreditAmount,
                    CreditAmount = l.DebitAmount,
                    Description = string.IsNullOrEmpty(l.Description) ? $"Rev {orig.VoucherNo}" : $"Rev: {l.Description}",
                    Currency = l.Currency ?? "PKR",
                    ExchangeRate = l.ExchangeRate ?? 1
                });
            }

            orig.IsReversed = true;
            orig.ReversedBy = CurrentUserId;
            orig.ReversedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = $"Reversal posted as {voucherNo}.";
            return RedirectToAction(nameof(Details), new { id = rev.VoucherID });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
