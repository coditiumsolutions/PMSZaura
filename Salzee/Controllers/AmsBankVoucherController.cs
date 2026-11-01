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
public class AmsBankVoucherController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "BPV", "BRV", "CPV", "CRV" };

    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;
    private readonly AmsAdminDeleteService _adminDelete;

    public AmsBankVoucherController(
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

    private async Task<int> GetTypeIdAsync(string typeCode) =>
        await _context.AccVoucherTypes.AsNoTracking()
            .Where(t => t.TypeCode == typeCode && t.IsActive)
            .Select(t => t.VoucherTypeID)
            .FirstOrDefaultAsync();

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

    private async Task LoadLookupsAsync(string typeCode)
    {
        ViewBag.TypeCode = typeCode.ToUpperInvariant();
        ViewBag.NextVoucherNo = await AmsVoucherNumberHelper.PeekNextAsync(_context, typeCode);
        ViewBag.OpenFiscalYears = await AmsAccountingPeriodGuard.GetOpenFiscalYearsAsync(_context);
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
        if (typeCode is "BPV" or "BRV")
        {
            ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.AccountHead)
                .OrderBy(b => b.BankName)
                .ToListAsync();
            ViewBag.PendingCheques = await _context.AccChequeRegisters.AsNoTracking()
                .Include(c => c.BankAccount)
                .Where(c => c.BankAccountID > 0 && c.VoucherID == null && c.Status == "Pending"
                    && !_context.AccARReceipts.Any(r => r.ChequeRegisterID == c.ChequeRegisterID))
                .OrderByDescending(c => c.EntryDate)
                .Take(200)
                .ToListAsync();
        }
    }

    private static void EnsureTwoLines(AmsBankVoucherCreateVm model)
    {
        while (model.Lines.Count < 2)
            model.Lines.Add(new AmsJvLineInputVm());
        if (model.Lines.Count > 2)
            model.Lines = model.Lines.Take(2).ToList();
    }

    private static List<AmsJvLineInputVm> BuildLinesFromVm(AmsBankVoucherCreateVm model, int bankGl, int cashGl)
    {
        EnsureTwoLines(model);
        var amount = model.Amount > 0
            ? model.Amount
            : Math.Max(model.Lines.Max(l => l.DebitAmount), model.Lines.Max(l => l.CreditAmount));
        var built = BuildLines(model.TypeCode, amount, bankGl, model.ContraAccountHeadID, cashGl);
        for (var i = 0; i < 2; i++)
        {
            if (model.Lines[i].DebitAmount > 0 || model.Lines[i].CreditAmount > 0)
            {
                built[i].DebitAmount = model.Lines[i].DebitAmount;
                built[i].CreditAmount = model.Lines[i].CreditAmount;
            }
            if (!string.IsNullOrWhiteSpace(model.Lines[i].Description))
                built[i].Description = model.Lines[i].Description.Trim();
        }

        return built;
    }

    private static List<AmsJvLineInputVm> BuildLines(string typeCode, decimal amount, int bankGl, int contraGl, int cashGl)
    {
        var lines = new List<AmsJvLineInputVm>();
        switch (typeCode.ToUpperInvariant())
        {
            case "BPV":
                lines.Add(new AmsJvLineInputVm { AccountHeadID = contraGl, DebitAmount = amount, CreditAmount = 0, Description = "BPV contra" });
                lines.Add(new AmsJvLineInputVm { AccountHeadID = bankGl, DebitAmount = 0, CreditAmount = amount, Description = "BPV bank" });
                break;
            case "BRV":
                lines.Add(new AmsJvLineInputVm { AccountHeadID = bankGl, DebitAmount = amount, CreditAmount = 0, Description = "BRV bank" });
                lines.Add(new AmsJvLineInputVm { AccountHeadID = contraGl, DebitAmount = 0, CreditAmount = amount, Description = "BRV contra" });
                break;
            case "CPV":
                lines.Add(new AmsJvLineInputVm { AccountHeadID = contraGl, DebitAmount = amount, CreditAmount = 0, Description = "CPV expense" });
                lines.Add(new AmsJvLineInputVm { AccountHeadID = cashGl, DebitAmount = 0, CreditAmount = amount, Description = "CPV cash" });
                break;
            case "CRV":
                lines.Add(new AmsJvLineInputVm { AccountHeadID = cashGl, DebitAmount = amount, CreditAmount = 0, Description = "CRV cash" });
                lines.Add(new AmsJvLineInputVm { AccountHeadID = contraGl, DebitAmount = 0, CreditAmount = amount, Description = "CRV contra" });
                break;
        }

        return lines;
    }

    private static void SyncLinesToModel(AmsBankVoucherCreateVm model, int bankGl, int cashGl)
    {
        var amount = model.Amount;
        if (amount <= 0)
            amount = Math.Max(model.Lines.Max(l => l.DebitAmount), model.Lines.Max(l => l.CreditAmount));
        var built = BuildLines(model.TypeCode, amount, bankGl, model.ContraAccountHeadID, cashGl);
        for (var i = 0; i < 2; i++)
        {
            model.Lines[i].AccountHeadID = built[i].AccountHeadID;
            model.Lines[i].DebitAmount = built[i].DebitAmount;
            model.Lines[i].CreditAmount = built[i].CreditAmount;
            model.Lines[i].Description = built[i].Description;
        }
    }

    private async Task ReserveOrCreateChequeAsync(AccVoucher v, AmsBankVoucherCreateVm model, int bankAccountId, string typeCode)
    {
        if (!model.UseCheque)
            return;
        var isBrv = typeCode.Equals("BRV", StringComparison.OrdinalIgnoreCase);
        if (model.ChequeRegisterID is int regId)
        {
            var reg = await _context.AccChequeRegisters.FirstOrDefaultAsync(c => c.ChequeRegisterID == regId && c.VoucherID == null);
            if (reg != null && reg.BankAccountID == bankAccountId)
            {
                reg.VoucherID = v.VoucherID;
                reg.Amount = model.Amount;
                reg.IsPostDated = model.IsPostDated;
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.NewChequeNo) && model.NewChequeDate.HasValue)
        {
            _context.AccChequeRegisters.Add(new AccChequeRegister
            {
                BankAccountID = bankAccountId,
                ChequeNo = model.NewChequeNo.Trim(),
                ChequeDate = model.NewChequeDate.Value.Date,
                EntryDate = DateTime.UtcNow.Date,
                IsPostDated = model.IsPostDated,
                ChequeType = isBrv ? "Receipt" : "Payment",
                Amount = model.Amount,
                Status = "Pending",
                VoucherID = v.VoucherID,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            });
        }
    }

    private async Task ClearChequeReservationAsync(int voucherId)
    {
        var regs = await _context.AccChequeRegisters.Where(c => c.VoucherID == voucherId).ToListAsync();
        foreach (var r in regs)
        {
            if (string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                r.VoucherID = null;
        }
    }

    private async Task FinalizeChequesOnPostAsync(AccVoucher v)
    {
        var regs = await _context.AccChequeRegisters.Where(c => c.VoucherID == v.VoucherID).ToListAsync();
        foreach (var r in regs)
        {
            r.Status = "Cleared";
            r.ClearanceDate = v.VoucherDate;
        }
    }

    public async Task<IActionResult> Index(string type = "BPV")
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;
        type = type.ToUpperInvariant();
        if (!AllowedTypes.Contains(type)) type = "BPV";

        var tid = await GetTypeIdAsync(type);
        if (tid == 0)
        {
            ViewBag.Error = $"Voucher type {type} missing in acc.VoucherType.";
            return View(new List<AccVoucher>());
        }

        var list = await _context.AccVouchers.AsNoTracking()
            .Where(x => x.VoucherTypeID == tid)
            .Include(x => x.Period)
            .OrderByDescending(x => x.VoucherDate).ThenByDescending(x => x.VoucherID)
            .ToListAsync();
        ViewBag.TypeCode = type;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string type = "BPV")
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        type = type.ToUpperInvariant();
        if (!AllowedTypes.Contains(type)) type = "BPV";
        await LoadLookupsAsync(type);
        var vm = new AmsBankVoucherCreateVm { TypeCode = type, VoucherDate = DateTime.UtcNow.Date };
        EnsureTwoLines(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AmsBankVoucherCreateVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var type = model.TypeCode.ToUpperInvariant();
        if (!AllowedTypes.Contains(type))
            ModelState.AddModelError(nameof(model.TypeCode), "Invalid voucher type.");

        var tid = await GetTypeIdAsync(type);
        if (tid == 0)
            ModelState.AddModelError(string.Empty, "Voucher type not configured.");

        EnsureTwoLines(model);
        if (model.Amount <= 0)
        {
            var lineAmt = model.Lines.Sum(l => l.DebitAmount + l.CreditAmount);
            if (lineAmt > 0)
                model.Amount = Math.Max(model.Lines.Max(l => l.DebitAmount), model.Lines.Max(l => l.CreditAmount));
        }
        if (model.Amount <= 0)
            ModelState.AddModelError(nameof(model.Amount), "Enter amount or debit/credit on voucher lines.");

        if (type is "BPV" or "BRV")
        {
            if (model.BankAccountID is null or <= 0)
                ModelState.AddModelError(nameof(model.BankAccountID), "Select a bank account.");
        }
        else
        {
            if (model.CashAccountHeadID <= 0 || model.ContraAccountHeadID <= 0)
                ModelState.AddModelError(string.Empty, "Select cash and contra GL accounts.");
        }

        if (model.ContraAccountHeadID <= 0 && type is "BPV" or "BRV")
            ModelState.AddModelError(nameof(model.ContraAccountHeadID), "Select contra GL account.");

        var (okPd, errPd, period) = await AmsAccountingPeriodGuard.ValidateFiscalYearAndDateAsync(
            _context, model.FiscalYearID, model.VoucherDate);
        if (!okPd)
            ModelState.AddModelError(nameof(model.FiscalYearID), errPd!);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(type);
            SyncLinesToModel(model, 0, type is "CPV" or "CRV" ? model.CashAccountHeadID : 0);
            return View(model);
        }

        async Task ReloadForViewAsync() => await LoadLookupsAsync(type);

        int bankGl = 0;
        int bankAccountId = 0;
        if (type is "BPV" or "BRV")
        {
            var bank = await _context.AccBankAccounts.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BankAccountID == model.BankAccountID!.Value);
            if (bank == null)
            {
                ModelState.AddModelError(nameof(model.BankAccountID), "Invalid bank account.");
                await LoadLookupsAsync(type);
                SyncLinesToModel(model, 0, 0);
                return View(model);
            }

            bankGl = bank.AccountHeadID;
            bankAccountId = bank.BankAccountID;
        }

        var cashGl = type is "CPV" or "CRV" ? model.CashAccountHeadID : 0;
        var lines = BuildLinesFromVm(model, bankGl, cashGl);
        model.Amount = Math.Max(lines.Max(l => l.DebitAmount), lines.Max(l => l.CreditAmount));
        var td = lines.Sum(x => x.DebitAmount);
        var tc = lines.Sum(x => x.CreditAmount);
        if (td != tc)
        {
            ModelState.AddModelError(string.Empty, "Total debits must equal total credits.");
            await LoadLookupsAsync(type);
            SyncLinesToModel(model, bankGl, cashGl);
            return View(model);
        }

        var voucherNo = await AmsVoucherNumberHelper.AllocateNextAsync(_context, type);
        var v = new AccVoucher
        {
            VoucherTypeID = tid,
            VoucherNo = voucherNo,
            VoucherDate = model.VoucherDate.Date,
            PeriodID = period!.PeriodID,
            FiscalYearID = period.FiscalYearID,
            ReferenceNo = model.ReferenceNo,
            Narration = model.Narration,
            TotalDebit = td,
            TotalCredit = tc,
            Status = "Draft",
            SourceModule = $"AMS.BankVoucher.{type}",
            BankAccountID = type is "BPV" or "BRV" ? bankAccountId : null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId
        };
        if (!await TrySaveVoucherHeaderAsync(v, type, ReloadForViewAsync))
        {
            SyncLinesToModel(model, bankGl, cashGl);
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

        if (type is "BPV" or "BRV")
            await ReserveOrCreateChequeAsync(v, model, bankAccountId, type);

        await _context.SaveChangesAsync();
        TempData["Success"] = $"{type} {voucherNo} saved as draft.";
        return RedirectToAction(nameof(Details), new { id = v.VoucherID });
    }

    public async Task<IActionResult> Details(int id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var v = await _context.AccVouchers.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(l => l.AccountHead)
            .Include(x => x.VoucherType)
            .Include(x => x.Period)
            .IncludeVoucherBankAccountIfMapped()
            .FirstOrDefaultAsync(x => x.VoucherID == id);
        if (v?.VoucherType == null || !AllowedTypes.Contains(v.VoucherType.TypeCode))
            return NotFound();
        ViewBag.LinkedCheques = await _context.AccChequeRegisters.AsNoTracking()
            .Where(c => c.VoucherID == id)
            .OrderBy(c => c.ChequeRegisterID)
            .ToListAsync();
        return View(v);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        var ok = await TransitionAsync(id, "Draft", "Pending", (v, _) =>
        {
            v.SubmittedBy = CurrentUserId;
            v.SubmittedAt = DateTime.UtcNow;
        });
        TempData[ok ? "Success" : "Error"] = ok ? "Submitted." : "Voucher not found or not in draft status.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        var ok = await TransitionAsync(id, "Pending", "Approved", (v, _) =>
        {
            v.ApprovedBy = CurrentUserId;
            v.ApprovedAt = DateTime.UtcNow;
        });
        TempData[ok ? "Success" : "Error"] = ok ? "Approved." : "Voucher not found or not pending approval.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var v = await _context.AccVouchers.Include(x => x.Lines).Include(x => x.VoucherType)
            .FirstOrDefaultAsync(x => x.VoucherID == id);
        if (v == null) return NotFound();
        if (!AllowedTypes.Contains(v.VoucherType!.TypeCode))
            return NotFound();
        if (!string.Equals(v.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only approved vouchers can be posted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var (ok, err, _) = await AmsAccountingPeriodGuard.ValidateOpenPeriodAsync(_context, v.PeriodID, v.VoucherDate);
        if (!ok)
        {
            TempData["Error"] = err;
            return RedirectToAction(nameof(Details), new { id });
        }

        v.Status = "Posted";
        v.PostedBy = CurrentUserId;
        v.PostedAt = DateTime.UtcNow;
        await FinalizeChequesOnPostAsync(v);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Posted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var denied = await EnsurePermissionAsync("Admin");
        if (denied != null) return denied;
        var v = await _context.AccVouchers.Include(x => x.VoucherType)
            .FirstOrDefaultAsync(x => x.VoucherID == id);
        if (v == null) return NotFound();
        if (!AllowedTypes.Contains(v.VoucherType!.TypeCode))
            return NotFound();

        await ClearChequeReservationAsync(id);
        var result = await _adminDelete.DeleteVoucherAsync(id);
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;
        return RedirectToAction(result.Success ? nameof(Index) : nameof(Details),
            result.Success ? new { type = v.VoucherType.TypeCode } : new { id });
    }

    private async Task<bool> TransitionAsync(int id, string fromStatus, string toStatus, Action<AccVoucher, AccVoucherType> mutator)
    {
        var v = await _context.AccVouchers.Include(x => x.VoucherType).FirstOrDefaultAsync(x => x.VoucherID == id);
        if (v == null || !AllowedTypes.Contains(v.VoucherType!.TypeCode))
            return false;
        if (!string.Equals(v.Status, fromStatus, StringComparison.OrdinalIgnoreCase))
            return false;
        v.Status = toStatus;
        mutator(v, v.VoucherType);
        await _context.SaveChangesAsync();
        return true;
    }
}
