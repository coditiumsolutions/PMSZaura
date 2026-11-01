using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsChequeRegisterController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsChequeRegisterController(PMSDbContext context, IModulePermissionService modulePermission)
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

    /// <summary>Read-only PDC / cheque register.</summary>
    public async Task<IActionResult> Index(string? status, int? bankAccountId, bool pdcOnly = false)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.BankName)
            .ToListAsync();
        ViewBag.FilterStatus = status;
        ViewBag.FilterBankId = bankAccountId;
        ViewBag.PdcOnly = pdcOnly;

        var q = _context.AccChequeRegisters.AsNoTracking()
            .Include(c => c.BankAccount)
            .Include(c => c.Voucher).ThenInclude(v => v!.VoucherType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(c => c.Status == status);
        if (bankAccountId is int bid && bid > 0)
            q = q.Where(c => c.BankAccountID == bid);
        if (pdcOnly)
            q = q.Where(c => c.IsPostDated);

        var cheques = await q
            .OrderByDescending(c => c.EntryDate)
            .ThenByDescending(c => c.ChequeRegisterID)
            .Take(500)
            .ToListAsync();

        var ids = cheques.Select(c => c.ChequeRegisterID).ToList();
        var receipts = ids.Count == 0
            ? new List<AccARReceipt>()
            : await _context.AccARReceipts.AsNoTracking()
                .Where(r => r.ChequeRegisterID != null && ids.Contains(r.ChequeRegisterID!.Value))
                .ToListAsync();
        var byCheque = receipts
            .GroupBy(r => r.ChequeRegisterID!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = cheques.Select(c => new AmsChequeRegisterListItemVm
        {
            Cheque = c,
            Receipt = byCheque.TryGetValue(c.ChequeRegisterID, out var r) ? r : null
        }).ToList();

        return View(rows);
    }
}
