using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsBankReconciliationController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsBankReconciliationController(PMSDbContext context, IModulePermissionService modulePermission)
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

        var list = await _context.AccBankReconciliations.AsNoTracking()
            .Include(r => r.BankAccount)
            .Include(r => r.Period)
            .OrderByDescending(r => r.StatementDate).ThenByDescending(r => r.ReconciliationID)
            .Take(200)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.Banks = await _context.AccBankAccounts.AsNoTracking()
            .Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
        ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
            .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
        return View(new AccBankReconciliation { StatementDate = DateTime.Today, Status = "Draft" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccBankReconciliation model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (!ModelState.IsValid)
        {
            ViewBag.Banks = await _context.AccBankAccounts.AsNoTracking()
                .Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
            ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
                .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
            return View(model);
        }

        model.ReconciliationID = 0;
        model.CreatedBy = DbUserId10(CurrentUserId);
        model.CreatedAt = DateTime.UtcNow;
        _context.AccBankReconciliations.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Bank reconciliation created.";
        return RedirectToAction(nameof(Details), new { id = model.ReconciliationID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var r = await _context.AccBankReconciliations.FirstOrDefaultAsync(x => x.ReconciliationID == id);
        if (r == null) return NotFound();
        ViewBag.Banks = await _context.AccBankAccounts.AsNoTracking()
            .Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
        ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
            .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
        return View(r);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccBankReconciliation model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.ReconciliationID) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Banks = await _context.AccBankAccounts.AsNoTracking()
                .Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
            ViewBag.Periods = await _context.AccAccountingPeriods.AsNoTracking()
                .OrderByDescending(p => p.StartDate).Take(120).ToListAsync();
            return View(model);
        }

        var r = await _context.AccBankReconciliations.FirstOrDefaultAsync(x => x.ReconciliationID == id);
        if (r == null) return NotFound();
        r.BankAccountID = model.BankAccountID;
        r.PeriodID = model.PeriodID;
        r.StatementDate = model.StatementDate;
        r.BankStatementBalance = model.BankStatementBalance;
        r.BookBalance = model.BookBalance;
        r.Status = model.Status;
        r.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        await _context.SaveChangesAsync();
        TempData["Success"] = "Reconciliation updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;
        await SetViewBagEditFlagsAsync();

        var r = await _context.AccBankReconciliations.AsNoTracking()
            .Include(x => x.BankAccount)
            .Include(x => x.Period)
            .Include(x => x.Lines).ThenInclude(l => l.VoucherLine)
            .Include(x => x.Lines).ThenInclude(l => l.ChequeRegister)
            .FirstOrDefaultAsync(x => x.ReconciliationID == id);
        if (r == null) return NotFound();

        ViewBag.VoucherLinePick = await _context.AccVoucherLines.AsNoTracking()
            .Include(vl => vl.Voucher).ThenInclude(v => v!.VoucherType)
            .Where(vl => vl.Voucher != null && vl.Voucher.Status == "Posted" && vl.Voucher.BankAccountID == r.BankAccountID)
            .OrderByDescending(vl => vl.Voucher!.VoucherDate)
            .Take(150)
            .ToListAsync();
        ViewBag.ChequePick = await _context.AccChequeRegisters.AsNoTracking()
            .Where(c => c.BankAccountID == r.BankAccountID)
            .OrderByDescending(c => c.EntryDate)
            .Take(100)
            .ToListAsync();

        return View(r);
    }

    [HttpGet]
    public async Task<IActionResult> AddLine(int reconciliationId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var r = await _context.AccBankReconciliations.AsNoTracking().FirstOrDefaultAsync(x => x.ReconciliationID == reconciliationId);
        if (r == null) return NotFound();
        ViewBag.ReconciliationId = reconciliationId;
        ViewBag.VoucherLinePick = await _context.AccVoucherLines.AsNoTracking()
            .Include(vl => vl.Voucher).ThenInclude(v => v!.VoucherType)
            .Where(vl => vl.Voucher != null && vl.Voucher.Status == "Posted" && vl.Voucher.BankAccountID == r.BankAccountID)
            .OrderByDescending(vl => vl.Voucher!.VoucherDate)
            .Take(150)
            .ToListAsync();
        ViewBag.ChequePick = await _context.AccChequeRegisters.AsNoTracking()
            .Where(c => c.BankAccountID == r.BankAccountID)
            .OrderByDescending(c => c.EntryDate)
            .Take(100)
            .ToListAsync();
        return View(new AccBankReconciliationLine
        {
            ReconciliationID = reconciliationId,
            TransactionDate = DateTime.Today
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(int reconciliationId, AccBankReconciliationLine model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (reconciliationId != model.ReconciliationID) return BadRequest();

        if (!await _context.AccBankReconciliations.AnyAsync(x => x.ReconciliationID == reconciliationId))
            return NotFound();

        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        if (model.Description != null && model.Description.Length > 300) model.Description = model.Description[..300];

        if (model.VoucherLineID is <= 0)
            model.VoucherLineID = null;
        if (model.ChequeRegisterID is <= 0)
            model.ChequeRegisterID = null;

        if (!ModelState.IsValid)
        {
            var r0 = await _context.AccBankReconciliations.AsNoTracking().FirstAsync(x => x.ReconciliationID == reconciliationId);
            ViewBag.ReconciliationId = reconciliationId;
            ViewBag.VoucherLinePick = await _context.AccVoucherLines.AsNoTracking()
                .Include(vl => vl.Voucher).ThenInclude(v => v!.VoucherType)
                .Where(vl => vl.Voucher != null && vl.Voucher.Status == "Posted" && vl.Voucher.BankAccountID == r0.BankAccountID)
                .OrderByDescending(vl => vl.Voucher!.VoucherDate).Take(150).ToListAsync();
            ViewBag.ChequePick = await _context.AccChequeRegisters.AsNoTracking()
                .Where(c => c.BankAccountID == r0.BankAccountID).OrderByDescending(c => c.EntryDate).Take(100).ToListAsync();
            return View(model);
        }

        model.ReconLineID = 0;
        _context.AccBankReconciliationLines.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Line added.";
        return RedirectToAction(nameof(Details), new { id = reconciliationId });
    }

    [HttpGet]
    public async Task<IActionResult> EditLine(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var line = await _context.AccBankReconciliationLines.Include(l => l.Reconciliation)
            .FirstOrDefaultAsync(l => l.ReconLineID == id);
        if (line?.Reconciliation == null) return NotFound();
        var r = line.Reconciliation;
        ViewBag.VoucherLinePick = await _context.AccVoucherLines.AsNoTracking()
            .Include(vl => vl.Voucher).ThenInclude(v => v!.VoucherType)
            .Where(vl => vl.Voucher != null && vl.Voucher.Status == "Posted" && vl.Voucher.BankAccountID == r.BankAccountID)
            .OrderByDescending(vl => vl.Voucher!.VoucherDate).Take(150).ToListAsync();
        ViewBag.ChequePick = await _context.AccChequeRegisters.AsNoTracking()
            .Where(c => c.BankAccountID == r.BankAccountID)
            .OrderByDescending(c => c.EntryDate).Take(100).ToListAsync();
        return View(line);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLine(int id, AccBankReconciliationLine model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.ReconLineID) return BadRequest();

        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        if (model.Description != null && model.Description.Length > 300) model.Description = model.Description[..300];
        if (model.VoucherLineID is <= 0)
            model.VoucherLineID = null;
        if (model.ChequeRegisterID is <= 0)
            model.ChequeRegisterID = null;

        if (!ModelState.IsValid)
        {
            var line0 = await _context.AccBankReconciliationLines.Include(l => l.Reconciliation)
                .FirstAsync(l => l.ReconLineID == id);
            var r = line0.Reconciliation!;
            ViewBag.VoucherLinePick = await _context.AccVoucherLines.AsNoTracking()
                .Include(vl => vl.Voucher).ThenInclude(v => v!.VoucherType)
                .Where(vl => vl.Voucher != null && vl.Voucher.Status == "Posted" && vl.Voucher.BankAccountID == r.BankAccountID)
                .OrderByDescending(vl => vl.Voucher!.VoucherDate).Take(150).ToListAsync();
            ViewBag.ChequePick = await _context.AccChequeRegisters.AsNoTracking()
                .Where(c => c.BankAccountID == r.BankAccountID).OrderByDescending(c => c.EntryDate).Take(100).ToListAsync();
            return View(model);
        }

        var line = await _context.AccBankReconciliationLines.FirstOrDefaultAsync(l => l.ReconLineID == id);
        if (line == null) return NotFound();
        var rid = line.ReconciliationID;
        line.TransactionDate = model.TransactionDate;
        line.Description = model.Description;
        line.Amount = model.Amount;
        line.VoucherLineID = model.VoucherLineID;
        line.ChequeRegisterID = model.ChequeRegisterID;
        line.IsReconciled = model.IsReconciled;
        line.ReconciledAt = model.ReconciledAt;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Line updated.";
        return RedirectToAction(nameof(Details), new { id = rid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLine(int id)
    {
        var denied = await EnsurePermissionAsync("Admin");
        if (denied != null) return denied;

        var line = await _context.AccBankReconciliationLines.AsNoTracking()
            .FirstOrDefaultAsync(l => l.ReconLineID == id);
        if (line == null) return NotFound();
        var rid = line.ReconciliationID;
        var delete = HttpContext.RequestServices.GetRequiredService<AmsAdminDeleteService>();
        var result = await delete.DeleteBankReconciliationLineAsync(id);
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = rid });
    }
}
