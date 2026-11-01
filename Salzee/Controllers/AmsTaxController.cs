using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsTaxController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsTaxController(PMSDbContext context, IModulePermissionService modulePermission)
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

    public static decimal ComputeTaxAmount(decimal taxableAmount, decimal ratePercent) =>
        Math.Round(taxableAmount * ratePercent / 100m, 2, MidpointRounding.AwayFromZero);

    public async Task<IActionResult> IndexTransactions()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccTaxTransactions.AsNoTracking()
            .Include(t => t.TaxType)
            .Include(t => t.Voucher)
            .Include(t => t.Period)
            .OrderByDescending(t => t.CreatedAt)
            .Take(500)
            .ToListAsync();
        return View("Transactions", list);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTransaction()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.TaxTypes = await _context.AccTaxTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.TaxCode)
            .ToListAsync();
        ViewBag.PostedVouchers = await _context.AccVouchers.AsNoTracking()
            .Include(v => v.VoucherType)
            .Where(v => v.Status == "Posted")
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.VoucherID)
            .Take(300)
            .ToListAsync();
        return View("TransactionCreate", new AmsTaxTransactionCreateVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransaction(AmsTaxTransactionCreateVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (model.TaxableAmount <= 0)
            ModelState.AddModelError(nameof(model.TaxableAmount), "Taxable amount must be positive.");

        var v = await _context.AccVouchers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.VoucherID == model.VoucherID);
        if (v == null || !string.Equals(v.Status, "Posted", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.VoucherID), "Select a posted voucher.");

        var tt = await _context.AccTaxTypes.FirstOrDefaultAsync(t => t.TaxTypeID == model.TaxTypeID && t.IsActive);
        if (tt == null)
            ModelState.AddModelError(nameof(model.TaxTypeID), "Invalid tax type.");

        if (!ModelState.IsValid)
        {
            ViewBag.TaxTypes = await _context.AccTaxTypes.AsNoTracking()
                .Where(t => t.IsActive).OrderBy(t => t.TaxCode).ToListAsync();
            ViewBag.PostedVouchers = await _context.AccVouchers.AsNoTracking()
                .Include(x => x.VoucherType)
                .Where(x => x.Status == "Posted")
                .OrderByDescending(x => x.VoucherDate).ThenByDescending(x => x.VoucherID).Take(300).ToListAsync();
            return View("TransactionCreate", model);
        }

        var taxAmt = ComputeTaxAmount(model.TaxableAmount, tt!.Rate);
        var subId = string.IsNullOrWhiteSpace(model.SubLedgerID) ? null : model.SubLedgerID.Trim();
        if (subId != null && subId.Length > 10)
            subId = subId[..10];

        var row = new AccTaxTransaction
        {
            VoucherID = v!.VoucherID,
            TaxTypeID = tt.TaxTypeID,
            TaxableAmount = model.TaxableAmount,
            TaxRate = tt.Rate,
            TaxAmount = taxAmt,
            SubLedgerType = string.IsNullOrWhiteSpace(model.SubLedgerType) ? null : model.SubLedgerType.Trim(),
            SubLedgerID = subId,
            PeriodID = v.PeriodID,
            ChallanNo = string.IsNullOrWhiteSpace(model.ChallanNo) ? null : model.ChallanNo.Trim(),
            DepositedDate = model.DepositedDate?.Date,
            CreatedAt = DateTime.UtcNow
        };
        _context.AccTaxTransactions.Add(row);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Tax transaction recorded ({taxAmt:N2}).";
        return RedirectToAction(nameof(IndexTransactions));
    }

    public async Task<IActionResult> WhtSummary()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var raw = await _context.AccTaxTransactions.AsNoTracking()
            .Include(t => t.TaxType)
            .Where(t => t.TaxType != null && t.TaxType.TaxCategory == "WHT")
            .ToListAsync();
        var rows = raw
            .GroupBy(t => new { t.TaxType!.TaxCode, t.TaxType.TaxName, t.TaxType.AppliesTo })
            .Select(g => new AmsTaxSummaryRowVm
            {
                TaxCode = g.Key.TaxCode,
                TaxName = g.Key.TaxName,
                AppliesTo = g.Key.AppliesTo,
                TotalTaxable = g.Sum(x => x.TaxableAmount),
                TotalTax = g.Sum(x => x.TaxAmount),
                LineCount = g.Count()
            })
            .OrderBy(r => r.TaxCode)
            .ToList();
        return View("WhtSummary", rows);
    }

    public async Task<IActionResult> GstInputOutput()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var raw = await _context.AccTaxTransactions.AsNoTracking()
            .Include(t => t.TaxType)
            .Where(t => t.TaxType != null && t.TaxType.TaxCategory == "GST")
            .ToListAsync();
        var rows = raw
            .GroupBy(t => new { t.TaxType!.TaxCode, t.TaxType.TaxName, t.TaxType.AppliesTo })
            .Select(g => new AmsTaxSummaryRowVm
            {
                TaxCode = g.Key.TaxCode,
                TaxName = g.Key.TaxName,
                AppliesTo = g.Key.AppliesTo,
                TotalTaxable = g.Sum(x => x.TaxableAmount),
                TotalTax = g.Sum(x => x.TaxAmount),
                LineCount = g.Count()
            })
            .OrderBy(r => r.TaxCode)
            .ToList();
        return View("GstInputOutput", rows);
    }
}
