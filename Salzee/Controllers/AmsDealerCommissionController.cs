using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsDealerCommissionController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsDealerCommissionController(PMSDbContext context, IModulePermissionService modulePermission)
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

    public static void ApplyWhtFromGrossAndRate(AccDealerCommissionVoucher row)
    {
        row.WHTAmount = Math.Round(row.GrossCommission * row.WHTRate / 100m, 2, MidpointRounding.AwayFromZero);
        row.NetPayable = Math.Round(row.GrossCommission - row.WHTAmount, 2, MidpointRounding.AwayFromZero);
    }

    private async Task LoadLookupsAsync()
    {
        ViewBag.PostedVouchers = await _context.AccVouchers.AsNoTracking()
            .Include(v => v.VoucherType)
            .Where(v => v.Status == "Posted")
            .OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.VoucherID)
            .Take(200)
            .ToListAsync();
        ViewBag.APPayments = await _context.AccAPPayments.AsNoTracking()
            .Include(p => p.Vendor)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.APPaymentID)
            .Take(150)
            .ToListAsync();
    }

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccDealerCommissionVouchers.AsNoTracking()
            .Include(x => x.AccountingVoucher)
            .Include(x => x.APPayment)
            .OrderByDescending(x => x.VoucherDate).ThenByDescending(x => x.CommissionVoucherID)
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
        return View(new AccDealerCommissionVoucher
        {
            VoucherDate = DateTime.Today,
            Status = "Pending",
            WHTRate = 0,
            GrossCommission = 0,
            WHTAmount = 0,
            NetPayable = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccDealerCommissionVoucher model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.VoucherNo = model.VoucherNo.Trim();
        if (await _context.AccDealerCommissionVouchers.AnyAsync(x => x.VoucherNo == model.VoucherNo))
            ModelState.AddModelError(nameof(model.VoucherNo), "Voucher number already exists.");

        if (model.GrossCommission < 0)
            ModelState.AddModelError(nameof(model.GrossCommission), "Gross commission cannot be negative.");
        if (model.WHTRate < 0 || model.WHTRate > 100)
            ModelState.AddModelError(nameof(model.WHTRate), "WHT rate must be between 0 and 100.");

        model.ProjectID = string.IsNullOrWhiteSpace(model.ProjectID) ? null : model.ProjectID.Trim();
        model.AllotmentID = string.IsNullOrWhiteSpace(model.AllotmentID) ? null : model.AllotmentID.Trim();
        model.PMSDealerPaymentID = string.IsNullOrWhiteSpace(model.PMSDealerPaymentID) ? null : model.PMSDealerPaymentID.Trim();
        if (model.ProjectID != null && model.ProjectID.Length > 10) model.ProjectID = model.ProjectID[..10];
        if (model.AllotmentID != null && model.AllotmentID.Length > 10) model.AllotmentID = model.AllotmentID[..10];
        if (model.PMSDealerPaymentID != null && model.PMSDealerPaymentID.Length > 10) model.PMSDealerPaymentID = model.PMSDealerPaymentID[..10];

        if (model.AccountingVoucherID is int vid && vid > 0)
        {
            if (!await _context.AccVouchers.AnyAsync(v => v.VoucherID == vid && v.Status == "Posted"))
                ModelState.AddModelError(nameof(model.AccountingVoucherID), "Select a posted accounting voucher or leave blank.");
        }
        else
            model.AccountingVoucherID = null;

        if (model.APPaymentID is int apid && apid > 0)
        {
            if (!await _context.AccAPPayments.AnyAsync(p => p.APPaymentID == apid))
                ModelState.AddModelError(nameof(model.APPaymentID), "Invalid AP payment.");
        }
        else
            model.APPaymentID = null;

        ApplyWhtFromGrossAndRate(model);
        if (model.NetPayable < 0)
            ModelState.AddModelError(nameof(model.NetPayable), "Net payable cannot be negative.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        model.CommissionVoucherID = 0;
        model.CreatedBy = DbUserId10(CurrentUserId);
        model.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(model.Status))
            model.Status = "Pending";
        _context.AccDealerCommissionVouchers.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Dealer commission voucher saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var row = await _context.AccDealerCommissionVouchers.FirstOrDefaultAsync(x => x.CommissionVoucherID == id);
        if (row == null) return NotFound();
        await LoadLookupsAsync();
        return View(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccDealerCommissionVoucher model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.CommissionVoucherID) return BadRequest();

        model.VoucherNo = model.VoucherNo.Trim();
        if (await _context.AccDealerCommissionVouchers.AnyAsync(x => x.VoucherNo == model.VoucherNo && x.CommissionVoucherID != id))
            ModelState.AddModelError(nameof(model.VoucherNo), "Voucher number already exists.");

        if (model.GrossCommission < 0)
            ModelState.AddModelError(nameof(model.GrossCommission), "Gross commission cannot be negative.");
        if (model.WHTRate < 0 || model.WHTRate > 100)
            ModelState.AddModelError(nameof(model.WHTRate), "WHT rate must be between 0 and 100.");

        model.ProjectID = string.IsNullOrWhiteSpace(model.ProjectID) ? null : model.ProjectID.Trim();
        if (model.ProjectID != null && model.ProjectID.Length > 10) model.ProjectID = model.ProjectID[..10];
        model.AllotmentID = string.IsNullOrWhiteSpace(model.AllotmentID) ? null : model.AllotmentID.Trim();
        if (model.AllotmentID != null && model.AllotmentID.Length > 10) model.AllotmentID = model.AllotmentID[..10];
        model.PMSDealerPaymentID = string.IsNullOrWhiteSpace(model.PMSDealerPaymentID) ? null : model.PMSDealerPaymentID.Trim();
        if (model.PMSDealerPaymentID != null && model.PMSDealerPaymentID.Length > 10) model.PMSDealerPaymentID = model.PMSDealerPaymentID[..10];

        if (model.AccountingVoucherID is int vid && vid > 0)
        {
            if (!await _context.AccVouchers.AnyAsync(v => v.VoucherID == vid && v.Status == "Posted"))
                ModelState.AddModelError(nameof(model.AccountingVoucherID), "Select a posted accounting voucher or leave blank.");
        }
        else
            model.AccountingVoucherID = null;

        if (model.APPaymentID is int apid && apid > 0)
        {
            if (!await _context.AccAPPayments.AnyAsync(p => p.APPaymentID == apid))
                ModelState.AddModelError(nameof(model.APPaymentID), "Invalid AP payment.");
        }
        else
            model.APPaymentID = null;

        ApplyWhtFromGrossAndRate(model);
        if (model.NetPayable < 0)
            ModelState.AddModelError(nameof(model.NetPayable), "Net payable cannot be negative.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        var row = await _context.AccDealerCommissionVouchers.FirstOrDefaultAsync(x => x.CommissionVoucherID == id);
        if (row == null) return NotFound();
        row.VoucherNo = model.VoucherNo;
        row.VoucherDate = model.VoucherDate;
        row.DealerID = model.DealerID;
        row.ProjectID = model.ProjectID;
        row.AllotmentID = model.AllotmentID;
        row.PMSDealerPaymentID = model.PMSDealerPaymentID;
        row.GrossCommission = model.GrossCommission;
        row.WHTRate = model.WHTRate;
        row.WHTAmount = model.WHTAmount;
        row.NetPayable = model.NetPayable;
        row.Status = string.IsNullOrWhiteSpace(model.Status) ? row.Status : model.Status.Trim();
        row.AccountingVoucherID = model.AccountingVoucherID;
        row.APPaymentID = model.APPaymentID;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Dealer commission voucher updated.";
        return RedirectToAction(nameof(Index));
    }
}
