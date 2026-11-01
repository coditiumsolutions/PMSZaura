using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Models.Acc;
using Salzee.Services;
using Salzee.Utilities;

namespace Salzee.Controllers;

[Authorize]
public class AmsArController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsArController(PMSDbContext context, IModulePermissionService modulePermission)
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

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

    private static string DbUserId10(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return "SYSTEM";
        return userId.Length <= 10 ? userId : userId[..10];
    }

    /// <summary>Link or create <see cref="AccChequeRegister"/> for an AR receipt (bank voucher pattern; <c>VoucherID</c> stays null).</summary>
    private async Task ReserveOrCreateChequeForReceiptAsync(AccARReceipt rec, AmsArReceiptCreateVm model)
    {
        if (!model.UseCheque)
            return;
        var bankId = model.BankAccountID!.Value;
        var bank = await _context.AccBankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.BankAccountID == bankId);
        var uid = DbUserId10(CurrentUserId);
        var ridStr = rec.ARReceiptID.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (ridStr.Length > 10)
            ridStr = ridStr[..10];

        if (model.ChequeRegisterID is int regId)
        {
            var reg = await _context.AccChequeRegisters.FirstOrDefaultAsync(c =>
                c.ChequeRegisterID == regId
                && c.VoucherID == null
                && c.BankAccountID == bankId
                && c.Status == "Pending");
            if (reg == null)
                return;
            var usedElsewhere = await _context.AccARReceipts.AnyAsync(r => r.ChequeRegisterID == regId && r.ARReceiptID != rec.ARReceiptID);
            if (usedElsewhere)
                return;

            reg.Amount = model.ReceivedAmount;
            reg.IsPostDated = model.IsPostDated;
            reg.SubLedgerType = "ARReceipt";
            reg.SubLedgerID = ridStr;
            rec.ChequeRegisterID = reg.ChequeRegisterID;
            rec.ChequeNo = reg.ChequeNo;
            rec.ChequeDate = reg.ChequeDate;
            rec.BankName = bank?.BankName;
        }
        else if (!string.IsNullOrWhiteSpace(model.NewChequeNo) && model.NewChequeDate.HasValue)
        {
            var reg = new AccChequeRegister
            {
                BankAccountID = bankId,
                ChequeNo = model.NewChequeNo.Trim(),
                ChequeDate = model.NewChequeDate.Value.Date,
                EntryDate = DateTime.UtcNow.Date,
                IsPostDated = model.IsPostDated,
                ChequeType = "Receipt",
                Amount = model.ReceivedAmount,
                Status = "Pending",
                VoucherID = null,
                SubLedgerType = "ARReceipt",
                SubLedgerID = ridStr,
                Remarks = $"AR receipt {rec.ReceiptNo}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = uid
            };
            _context.AccChequeRegisters.Add(reg);
            await _context.SaveChangesAsync();
            rec.ChequeRegisterID = reg.ChequeRegisterID;
            rec.ChequeNo = reg.ChequeNo;
            rec.ChequeDate = reg.ChequeDate;
            rec.BankName = bank?.BankName;
        }
    }

    private async Task RecalcInvoiceStatusAsync(int arInvoiceId)
    {
        var inv = await _context.AccARInvoices.FirstOrDefaultAsync(i => i.ARInvoiceID == arInvoiceId);
        if (inv == null) return;
        var paid = await _context.AccARReceiptAllocations.Where(a => a.ARInvoiceID == arInvoiceId).SumAsync(a => a.AllocatedAmount);
        inv.PaidAmount = paid;
        if (paid >= inv.TotalAmount - 0.01m)
            inv.Status = "Paid";
        else if (paid > 0.01m)
            inv.Status = "PartiallyPaid";
        else
            inv.Status = "Unpaid";
    }

    public async Task<IActionResult> IndexInvoices()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccARInvoices.AsNoTracking()
            .Include(i => i.AccountHead)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.ARInvoiceID)
            .ToListAsync();
        return View("Invoices", list);
    }

    [HttpGet]
    public async Task<IActionResult> CreateInvoice()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(c => c.CustomerID).Take(500).ToListAsync();
        ViewBag.ArHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
        return View(new AccARInvoice
        {
            InvoiceDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            InvoiceType = "Misc",
            Status = "Unpaid"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(AccARInvoice model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.CustomerID = model.CustomerID.Trim();
        model.TotalAmount = model.SubTotal + model.TaxAmount - model.DiscountAmount;
        if (model.TotalAmount < 0)
            ModelState.AddModelError(string.Empty, "Total amount cannot be negative.");

        if (await _context.AccARInvoices.AnyAsync(i => i.InvoiceNo == model.InvoiceNo))
            ModelState.AddModelError(nameof(model.InvoiceNo), "Invoice number already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(c => c.CustomerID).Take(500).ToListAsync();
            ViewBag.ArHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
            return View(model);
        }

        model.ARInvoiceID = 0;
        model.PaidAmount = 0;
        model.CreatedAt = DateTime.UtcNow;
        model.CreatedBy = CurrentUserId;
        _context.AccARInvoices.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "AR invoice created.";
        return RedirectToAction(nameof(IndexInvoices));
    }

    [HttpGet]
    public async Task<IActionResult> CreateReceipt(string? customerId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(c => c.CustomerID).Take(500).ToListAsync();
        ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
        ViewBag.PendingCheques = await _context.AccChequeRegisters.AsNoTracking()
            .Include(c => c.BankAccount)
            .Where(c => c.BankAccountID > 0 && c.VoucherID == null && c.Status == "Pending"
                && !_context.AccARReceipts.Any(r => r.ChequeRegisterID == c.ChequeRegisterID))
            .OrderByDescending(c => c.EntryDate)
            .Take(200)
            .ToListAsync();
        var cust = (customerId ?? "").Trim();
        var openInvoices = await _context.AccARInvoices.AsNoTracking()
            .Where(i => i.CustomerID == cust && i.Status != "Paid" && i.TotalAmount > i.PaidAmount)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
        ViewBag.OpenInvoices = openInvoices;
        var vm = new AmsArReceiptCreateVm { CustomerID = cust, ReceiptDate = DateTime.UtcNow.Date, Allocations = openInvoices.Select(i => new AmsAllocationLineVm { ARInvoiceID = i.ARInvoiceID, Amount = 0 }).ToList() };
        return View("ReceiptCreate", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReceipt(AmsArReceiptCreateVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.CustomerID = (model.CustomerID ?? string.Empty).Trim();
        var alloc = model.Allocations.Where(a => a.Amount > 0).ToList();
        var sum = alloc.Sum(a => a.Amount);
        if (string.IsNullOrWhiteSpace(model.CustomerID))
            ModelState.AddModelError(nameof(model.CustomerID), "Select a customer.");
        if (sum <= 0)
            ModelState.AddModelError(string.Empty, "Allocate to at least one invoice.");
        if (Math.Abs(sum - model.ReceivedAmount) > 0.05m)
            ModelState.AddModelError(string.Empty, "Allocated total must match received amount.");

        foreach (var a in alloc)
        {
            var inv = await _context.AccARInvoices.FirstOrDefaultAsync(i => i.ARInvoiceID == a.ARInvoiceID && i.CustomerID == model.CustomerID);
            if (inv == null)
                ModelState.AddModelError(string.Empty, "Invalid invoice for customer.");
            else
            {
                var bal = inv.TotalAmount - inv.PaidAmount;
                if (a.Amount > bal + 0.05m)
                    ModelState.AddModelError(string.Empty, $"Allocation exceeds balance on {inv.InvoiceNo}.");
            }
        }

        if (model.UseCheque)
        {
            if (model.BankAccountID is null or <= 0)
                ModelState.AddModelError(nameof(model.BankAccountID), "Select a bank account when recording a cheque.");
            else
            {
                var hasReg = model.ChequeRegisterID is > 0;
                var hasNew = !string.IsNullOrWhiteSpace(model.NewChequeNo) && model.NewChequeDate.HasValue;
                if (!hasReg && !hasNew)
                    ModelState.AddModelError(string.Empty, "Select a pending cheque or enter new cheque number and date.");
                if (hasReg && model.ChequeRegisterID is int crid)
                {
                    if (await _context.AccARReceipts.AnyAsync(r => r.ChequeRegisterID == crid))
                        ModelState.AddModelError(nameof(model.ChequeRegisterID), "That cheque is already linked to an AR receipt.");
                    var regOk = await _context.AccChequeRegisters.AnyAsync(c =>
                        c.ChequeRegisterID == crid
                        && c.VoucherID == null
                        && c.BankAccountID == model.BankAccountID
                        && c.Status == "Pending");
                    if (!regOk)
                        ModelState.AddModelError(nameof(model.ChequeRegisterID), "Invalid or unavailable pending cheque for this bank.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(c => c.CustomerID).Take(500).ToListAsync();
            ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
            ViewBag.PendingCheques = await _context.AccChequeRegisters.AsNoTracking()
                .Include(c => c.BankAccount)
                .Where(c => c.BankAccountID > 0 && c.VoucherID == null && c.Status == "Pending"
                    && !_context.AccARReceipts.Any(r => r.ChequeRegisterID == c.ChequeRegisterID))
                .OrderByDescending(c => c.EntryDate)
                .Take(200)
                .ToListAsync();
            var openList = await _context.AccARInvoices.AsNoTracking()
                .Where(i => i.CustomerID == model.CustomerID && i.Status != "Paid" && i.TotalAmount > i.PaidAmount)
                .OrderBy(i => i.DueDate).ToListAsync();
            ViewBag.OpenInvoices = openList;
            var byId = model.Allocations.ToDictionary(a => a.ARInvoiceID, a => a.Amount);
            model.Allocations = openList.Select(i => new AmsAllocationLineVm { ARInvoiceID = i.ARInvoiceID, Amount = byId.TryGetValue(i.ARInvoiceID, out var a) ? a : 0 }).ToList();
            return View("ReceiptCreate", model);
        }

        var receiptNo = $"RC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var rec = new AccARReceipt
        {
            ReceiptNo = receiptNo,
            ReceiptDate = model.ReceiptDate.Date,
            CustomerID = model.CustomerID,
            ReceivedAmount = model.ReceivedAmount,
            PaymentMode = model.PaymentMode,
            BankAccountID = model.BankAccountID,
            IsPostDated = model.IsPostDated,
            Remarks = model.Remarks,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId
        };
        _context.AccARReceipts.Add(rec);
        await _context.SaveChangesAsync();

        await ReserveOrCreateChequeForReceiptAsync(rec, model);

        foreach (var a in alloc)
        {
            _context.AccARReceiptAllocations.Add(new AccARReceiptAllocation
            {
                ARReceiptID = rec.ARReceiptID,
                ARInvoiceID = a.ARInvoiceID,
                AllocatedAmount = a.Amount,
                AllocatedAt = DateTime.UtcNow,
                AllocatedBy = CurrentUserId
            });
            await RecalcInvoiceStatusAsync(a.ARInvoiceID);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Receipt {receiptNo} saved.";
        return RedirectToAction(nameof(IndexInvoices));
    }

    public async Task<IActionResult> Aging()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var today = DateTime.UtcNow.Date;
        var raw = await _context.AccARInvoices.AsNoTracking()
            .Where(i => i.Status != "Paid" && i.TotalAmount > i.PaidAmount)
            .OrderBy(i => i.CustomerID).ThenBy(i => i.DueDate)
            .ToListAsync();
        var rows = raw.Select(i => new AmsArAgingRowVm
        {
            ARInvoiceId = i.ARInvoiceID,
            InvoiceNo = i.InvoiceNo,
            CustomerId = i.CustomerID,
            DueDate = i.DueDate,
            Total = i.TotalAmount,
            Paid = i.PaidAmount,
            Balance = i.TotalAmount - i.PaidAmount,
            DaysPastDue = (int)(today - i.DueDate).TotalDays
        }).ToList();
        return View("Aging", rows);
    }

    public IActionResult IntegrationSketch()
    {
        return View();
    }
}
