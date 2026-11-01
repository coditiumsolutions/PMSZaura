using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsRefundVoucherController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsRefundVoucherController(PMSDbContext context, IModulePermissionService modulePermission)
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

    public static decimal ComputeNetRefund(decimal gross, decimal processing, decimal penalty, decimal other) =>
        Math.Round(gross - processing - penalty - other, 2, MidpointRounding.AwayFromZero);

    private async Task LoadLookupsAsync()
    {
        ViewBag.PostedVouchers = await _context.AccVouchers.AsNoTracking()
            .Include(v => v.VoucherType)
            .Where(v => v.Status == "Posted")
            .OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.VoucherID)
            .Take(200)
            .ToListAsync();
        ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.BankName).ThenBy(b => b.AccountNumber)
            .ToListAsync();
        ViewBag.ChequeRegisters = await _context.AccChequeRegisters.AsNoTracking()
            .Include(c => c.BankAccount)
            .OrderByDescending(c => c.EntryDate).ThenByDescending(c => c.ChequeRegisterID)
            .Take(200)
            .ToListAsync();
    }

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccRefundVouchers.AsNoTracking()
            .Include(x => x.AccountingVoucher)
            .Include(x => x.BankAccount)
            .OrderByDescending(x => x.VoucherDate).ThenByDescending(x => x.RefundVoucherID)
            .Take(300)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        await LoadLookupsAsync();
        return View(new AccRefundVoucher
        {
            VoucherDate = DateTime.Today,
            Status = "Pending",
            PaymentMode = "Bank",
            GrossRefundAmount = 0,
            ProcessingFee = 0,
            PenaltyDeduction = 0,
            OtherDeduction = 0,
            NetRefundAmount = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccRefundVoucher model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.VoucherNo = model.VoucherNo.Trim();
        if (await _context.AccRefundVouchers.AnyAsync(x => x.VoucherNo == model.VoucherNo))
            ModelState.AddModelError(nameof(model.VoucherNo), "Voucher number already exists.");

        model.CustomerID = model.CustomerID.Trim();
        if (model.CustomerID.Length > 10) model.CustomerID = model.CustomerID[..10];

        model.AllotmentID = string.IsNullOrWhiteSpace(model.AllotmentID) ? null : model.AllotmentID.Trim();
        if (model.AllotmentID != null && model.AllotmentID.Length > 10) model.AllotmentID = model.AllotmentID[..10];
        model.PMSRefundID = string.IsNullOrWhiteSpace(model.PMSRefundID) ? null : model.PMSRefundID.Trim();
        if (model.PMSRefundID != null && model.PMSRefundID.Length > 10) model.PMSRefundID = model.PMSRefundID[..10];

        if (model.ProcessingFee < 0 || model.PenaltyDeduction < 0 || model.OtherDeduction < 0)
            ModelState.AddModelError("", "Deductions cannot be negative.");

        model.NetRefundAmount = ComputeNetRefund(model.GrossRefundAmount, model.ProcessingFee, model.PenaltyDeduction, model.OtherDeduction);
        if (model.NetRefundAmount < 0)
            ModelState.AddModelError(nameof(model.NetRefundAmount), "Net refund cannot be negative.");

        model.PaymentMode = string.IsNullOrWhiteSpace(model.PaymentMode) ? "Bank" : model.PaymentMode.Trim();
        if (model.PaymentMode.Length > 30) model.PaymentMode = model.PaymentMode[..30];

        model.ChequeNo = string.IsNullOrWhiteSpace(model.ChequeNo) ? null : model.ChequeNo.Trim();
        if (model.ChequeNo != null && model.ChequeNo.Length > 30) model.ChequeNo = model.ChequeNo[..30];

        if (model.AccountingVoucherID is int vid && vid > 0)
        {
            if (!await _context.AccVouchers.AnyAsync(v => v.VoucherID == vid && v.Status == "Posted"))
                ModelState.AddModelError(nameof(model.AccountingVoucherID), "Select a posted accounting voucher or leave blank.");
        }
        else
            model.AccountingVoucherID = null;

        if (model.BankAccountID is int bid && bid > 0)
        {
            if (!await _context.AccBankAccounts.AnyAsync(b => b.BankAccountID == bid))
                ModelState.AddModelError(nameof(model.BankAccountID), "Invalid bank account.");
        }
        else
            model.BankAccountID = null;

        if (model.ChequeRegisterID is int cid && cid > 0)
        {
            if (!await _context.AccChequeRegisters.AnyAsync(c => c.ChequeRegisterID == cid))
                ModelState.AddModelError(nameof(model.ChequeRegisterID), "Invalid cheque register row.");
        }
        else
            model.ChequeRegisterID = null;

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        model.RefundVoucherID = 0;
        model.CreatedBy = DbUserId10(CurrentUserId);
        model.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(model.Status))
            model.Status = "Pending";
        _context.AccRefundVouchers.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Refund voucher saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var row = await _context.AccRefundVouchers.FirstOrDefaultAsync(x => x.RefundVoucherID == id);
        if (row == null) return NotFound();
        await LoadLookupsAsync();
        return View(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccRefundVoucher model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.RefundVoucherID) return BadRequest();

        model.VoucherNo = model.VoucherNo.Trim();
        if (await _context.AccRefundVouchers.AnyAsync(x => x.VoucherNo == model.VoucherNo && x.RefundVoucherID != id))
            ModelState.AddModelError(nameof(model.VoucherNo), "Voucher number already exists.");

        model.CustomerID = model.CustomerID.Trim();
        if (model.CustomerID.Length > 10) model.CustomerID = model.CustomerID[..10];

        model.AllotmentID = string.IsNullOrWhiteSpace(model.AllotmentID) ? null : model.AllotmentID.Trim();
        if (model.AllotmentID != null && model.AllotmentID.Length > 10) model.AllotmentID = model.AllotmentID[..10];
        model.PMSRefundID = string.IsNullOrWhiteSpace(model.PMSRefundID) ? null : model.PMSRefundID.Trim();
        if (model.PMSRefundID != null && model.PMSRefundID.Length > 10) model.PMSRefundID = model.PMSRefundID[..10];

        if (model.ProcessingFee < 0 || model.PenaltyDeduction < 0 || model.OtherDeduction < 0)
            ModelState.AddModelError("", "Deductions cannot be negative.");

        model.NetRefundAmount = ComputeNetRefund(model.GrossRefundAmount, model.ProcessingFee, model.PenaltyDeduction, model.OtherDeduction);
        if (model.NetRefundAmount < 0)
            ModelState.AddModelError(nameof(model.NetRefundAmount), "Net refund cannot be negative.");

        model.PaymentMode = string.IsNullOrWhiteSpace(model.PaymentMode) ? "Bank" : model.PaymentMode.Trim();
        if (model.PaymentMode.Length > 30) model.PaymentMode = model.PaymentMode[..30];

        model.ChequeNo = string.IsNullOrWhiteSpace(model.ChequeNo) ? null : model.ChequeNo.Trim();
        if (model.ChequeNo != null && model.ChequeNo.Length > 30) model.ChequeNo = model.ChequeNo[..30];

        if (model.AccountingVoucherID is int vid && vid > 0)
        {
            if (!await _context.AccVouchers.AnyAsync(v => v.VoucherID == vid && v.Status == "Posted"))
                ModelState.AddModelError(nameof(model.AccountingVoucherID), "Select a posted accounting voucher or leave blank.");
        }
        else
            model.AccountingVoucherID = null;

        if (model.BankAccountID is int bid && bid > 0)
        {
            if (!await _context.AccBankAccounts.AnyAsync(b => b.BankAccountID == bid))
                ModelState.AddModelError(nameof(model.BankAccountID), "Invalid bank account.");
        }
        else
            model.BankAccountID = null;

        if (model.ChequeRegisterID is int cid && cid > 0)
        {
            if (!await _context.AccChequeRegisters.AnyAsync(c => c.ChequeRegisterID == cid))
                ModelState.AddModelError(nameof(model.ChequeRegisterID), "Invalid cheque register row.");
        }
        else
            model.ChequeRegisterID = null;

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        var row = await _context.AccRefundVouchers.FirstOrDefaultAsync(x => x.RefundVoucherID == id);
        if (row == null) return NotFound();
        row.VoucherNo = model.VoucherNo;
        row.VoucherDate = model.VoucherDate;
        row.CustomerID = model.CustomerID;
        row.AllotmentID = model.AllotmentID;
        row.PMSRefundID = model.PMSRefundID;
        row.GrossRefundAmount = model.GrossRefundAmount;
        row.ProcessingFee = model.ProcessingFee;
        row.PenaltyDeduction = model.PenaltyDeduction;
        row.OtherDeduction = model.OtherDeduction;
        row.NetRefundAmount = model.NetRefundAmount;
        row.PaymentMode = model.PaymentMode;
        row.BankAccountID = model.BankAccountID;
        row.ChequeRegisterID = model.ChequeRegisterID;
        row.ChequeNo = model.ChequeNo;
        row.ChequeDate = model.ChequeDate;
        row.Status = string.IsNullOrWhiteSpace(model.Status) ? row.Status : model.Status.Trim();
        row.AccountingVoucherID = model.AccountingVoucherID;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Refund voucher updated.";
        return RedirectToAction(nameof(Index));
    }
}
