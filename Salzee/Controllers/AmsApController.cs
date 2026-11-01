using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsApController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsApController(PMSDbContext context, IModulePermissionService modulePermission)
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

    private static string DbUserId10(string? userId) =>
        string.IsNullOrEmpty(userId) ? "SYSTEM" : userId.Length <= 10 ? userId : userId[..10];

    private static decimal BillPayableBalance(AccAPBill b) =>
        b.TotalAmount - b.RetentionAmount - b.PaidAmount;

    private async Task RecalcApBillAsync(int apBillId)
    {
        var bill = await _context.AccAPBills.FirstOrDefaultAsync(b => b.APBillID == apBillId);
        if (bill == null) return;
        var allocs = await _context.AccAPPaymentAllocations.Where(a => a.APBillID == apBillId).ToListAsync();
        bill.PaidAmount = allocs.Where(a => !a.IsRetentionRelease).Sum(a => a.AllocatedAmount);
        bill.RetentionReleased = allocs.Where(a => a.IsRetentionRelease).Sum(a => a.AllocatedAmount);
        var bal = BillPayableBalance(bill);
        if (bal <= 0.01m)
            bill.Status = "Paid";
        else if (bill.PaidAmount > 0.01m || bill.RetentionReleased > 0.01m)
            bill.Status = "PartiallyPaid";
        else if (string.Equals(bill.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        { /* unchanged */ }
        else
            bill.Status = "Approved";
    }

    public async Task<IActionResult> IndexBills()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccAPBills.AsNoTracking()
            .Include(b => b.Vendor)
            .OrderByDescending(b => b.BillDate).ThenByDescending(b => b.APBillID)
            .ToListAsync();
        return View("Bills", list);
    }

    private static AmsApBillCreateVm BuildEmptyBillVm()
    {
        var lines = Enumerable.Range(0, 8).Select(_ => new AmsApBillLineInputVm()).ToList();
        return new AmsApBillCreateVm { Lines = lines, BillDate = DateTime.UtcNow.Date, DueDate = DateTime.UtcNow.Date.AddDays(30) };
    }

    [HttpGet]
    public async Task<IActionResult> CreateBill()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.Vendors = await _context.AccVendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.VendorCode).ToListAsync();
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
        ViewBag.Projects = await _context.Projects.AsNoTracking().OrderBy(p => p.ProjectID).Take(300).ToListAsync();
        return View("CreateBill", BuildEmptyBillVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBill(AmsApBillCreateVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.BillNo = model.BillNo.Trim();
        var lines = model.Lines
            .Where(l => l.AccountHeadID > 0 && l.Amount > 0)
            .Select((l, idx) => new AmsApBillLineInputVm
            {
                AccountHeadID = l.AccountHeadID,
                Description = string.IsNullOrWhiteSpace(l.Description) ? "Line" : l.Description.Trim(),
                Amount = l.Amount
            }).ToList();

        if (!lines.Any())
            ModelState.AddModelError(string.Empty, "Enter at least one line with account and amount.");
        if (await _context.AccAPBills.AnyAsync(b => b.BillNo == model.BillNo))
            ModelState.AddModelError(nameof(model.BillNo), "Bill number already exists.");
        if (!await _context.AccVendors.AnyAsync(v => v.VendorID == model.VendorID && v.IsActive))
            ModelState.AddModelError(nameof(model.VendorID), "Invalid vendor.");

        var subTotal = lines.Sum(l => l.Amount);
        var total = subTotal + model.WHTAmount + model.GSTAmount + model.OtherTaxAmount;
        if (model.RetentionAmount > total + 0.01m)
            ModelState.AddModelError(nameof(model.RetentionAmount), "Retention cannot exceed bill total.");

        if (!ModelState.IsValid)
        {
            while (model.Lines.Count < 8)
                model.Lines.Add(new AmsApBillLineInputVm());
            ViewBag.Vendors = await _context.AccVendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.VendorCode).ToListAsync();
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
            ViewBag.Projects = await _context.Projects.AsNoTracking().OrderBy(p => p.ProjectID).Take(300).ToListAsync();
            return View("CreateBill", model);
        }

        var bill = new AccAPBill
        {
            BillNo = model.BillNo,
            BillDate = model.BillDate.Date,
            DueDate = model.DueDate.Date,
            VendorID = model.VendorID,
            ProjectID = string.IsNullOrWhiteSpace(model.ProjectID) ? null : model.ProjectID.Trim(),
            BillType = string.IsNullOrWhiteSpace(model.BillType) ? "Invoice" : model.BillType.Trim(),
            SubTotal = subTotal,
            WHTAmount = model.WHTAmount,
            GSTAmount = model.GSTAmount,
            OtherTaxAmount = model.OtherTaxAmount,
            TotalAmount = total,
            RetentionAmount = model.RetentionAmount,
            RetentionReleased = 0,
            PaidAmount = 0,
            Status = "Draft",
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = DbUserId10(CurrentUserId)
        };
        _context.AccAPBills.Add(bill);
        await _context.SaveChangesAsync();

        short n = 1;
        foreach (var l in lines)
        {
            _context.AccAPBillLines.Add(new AccAPBillLine
            {
                APBillID = bill.APBillID,
                LineNumber = n++,
                AccountHeadID = l.AccountHeadID,
                Description = l.Description,
                Amount = l.Amount
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Bill {bill.BillNo} saved as draft.";
        return RedirectToAction(nameof(BillDetails), new { id = bill.APBillID });
    }

    public async Task<IActionResult> BillDetails(int id)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var b = await _context.AccAPBills
            .Include(x => x.Vendor)
            .Include(x => x.Lines).ThenInclude(l => l.AccountHead)
            .FirstOrDefaultAsync(x => x.APBillID == id);
        if (b == null) return NotFound();
        return View(b);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBill(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var b = await _context.AccAPBills.FirstOrDefaultAsync(x => x.APBillID == id);
        if (b == null) return NotFound();
        if (!string.Equals(b.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only draft bills can be approved.";
            return RedirectToAction(nameof(BillDetails), new { id });
        }

        b.Status = "Approved";
        b.ApprovedBy = DbUserId10(CurrentUserId);
        b.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Bill approved for payment.";
        return RedirectToAction(nameof(BillDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> CreatePayment(int? vendorId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var vid = vendorId ?? 0;
        ViewBag.Vendors = await _context.AccVendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.VendorCode).ToListAsync();
        ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
        var open = vid == 0
            ? new List<AccAPBill>()
            : await _context.AccAPBills.AsNoTracking()
                .Include(b => b.Vendor)
                .Where(b => b.VendorID == vid
                    && (b.Status == "Approved" || b.Status == "PartiallyPaid")
                    && b.TotalAmount - b.RetentionAmount - b.PaidAmount > 0.01m)
                .OrderBy(b => b.DueDate)
                .ToListAsync();
        ViewBag.OpenBills = open;
        var vm = new AmsApPaymentCreateVm
        {
            VendorID = vid,
            PaymentDate = DateTime.UtcNow.Date,
            Allocations = open.Select(b => new AmsApAllocationLineVm { APBillID = b.APBillID, Amount = 0, IsRetentionRelease = false }).ToList()
        };
        return View("PaymentCreate", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment(AmsApPaymentCreateVm model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var alloc = model.Allocations.Where(a => a.Amount > 0).ToList();
        var sum = alloc.Sum(a => a.Amount);
        if (model.VendorID <= 0)
            ModelState.AddModelError(nameof(model.VendorID), "Select a vendor.");
        if (sum <= 0)
            ModelState.AddModelError(string.Empty, "Allocate to at least one bill.");
        if (Math.Abs(sum - model.PaidAmount) > 0.05m)
            ModelState.AddModelError(string.Empty, "Allocated total must match payment amount.");

        foreach (var a in alloc)
        {
            var bill = await _context.AccAPBills.FirstOrDefaultAsync(b => b.APBillID == a.APBillID && b.VendorID == model.VendorID);
            if (bill == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid bill for vendor.");
                continue;
            }

            if (!string.Equals(bill.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(bill.Status, "PartiallyPaid", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, $"Bill {bill.BillNo} is not approved for payment.");
                continue;
            }

            if (a.IsRetentionRelease)
            {
                var alreadyRel = await _context.AccAPPaymentAllocations
                    .Where(x => x.APBillID == bill.APBillID && x.IsRetentionRelease)
                    .SumAsync(x => (decimal?)x.AllocatedAmount) ?? 0m;
                var left = bill.RetentionAmount - alreadyRel;
                if (a.Amount > left + 0.05m)
                    ModelState.AddModelError(string.Empty, $"Retention release exceeds remaining retention on {bill.BillNo}.");
            }
            else
            {
                var bal = BillPayableBalance(bill);
                if (a.Amount > bal + 0.05m)
                    ModelState.AddModelError(string.Empty, $"Allocation exceeds payable balance on {bill.BillNo}.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.AccVendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.VendorCode).ToListAsync();
            ViewBag.BankAccounts = await _context.AccBankAccounts.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.BankName).ToListAsync();
            var openList = await _context.AccAPBills.AsNoTracking()
                .Include(b => b.Vendor)
                .Where(b => b.VendorID == model.VendorID
                    && (b.Status == "Approved" || b.Status == "PartiallyPaid")
                    && b.TotalAmount - b.RetentionAmount - b.PaidAmount > 0.01m)
                .OrderBy(b => b.DueDate).ToListAsync();
            ViewBag.OpenBills = openList;
            var byId = model.Allocations.ToDictionary(x => x.APBillID, x => (x.Amount, x.IsRetentionRelease));
            model.Allocations = openList.Select(b =>
            {
                var t = byId.TryGetValue(b.APBillID, out var p) ? p : (0m, false);
                return new AmsApAllocationLineVm { APBillID = b.APBillID, Amount = t.Item1, IsRetentionRelease = t.Item2 };
            }).ToList();
            return View("PaymentCreate", model);
        }

        var payNo = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var pay = new AccAPPayment
        {
            PaymentNo = payNo,
            PaymentDate = model.PaymentDate.Date,
            VendorID = model.VendorID,
            PaidAmount = model.PaidAmount,
            PaymentMode = model.PaymentMode,
            BankAccountID = model.BankAccountID,
            Status = "Posted",
            Remarks = model.Remarks,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = DbUserId10(CurrentUserId)
        };
        _context.AccAPPayments.Add(pay);
        await _context.SaveChangesAsync();

        var uid = DbUserId10(CurrentUserId);
        foreach (var a in alloc)
        {
            _context.AccAPPaymentAllocations.Add(new AccAPPaymentAllocation
            {
                APPaymentID = pay.APPaymentID,
                APBillID = a.APBillID,
                AllocatedAmount = a.Amount,
                IsRetentionRelease = a.IsRetentionRelease,
                AllocatedAt = DateTime.UtcNow,
                AllocatedBy = uid
            });
            await RecalcApBillAsync(a.APBillID);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Payment {payNo} recorded.";
        return RedirectToAction(nameof(IndexBills));
    }

    public async Task<IActionResult> Aging()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var today = DateTime.UtcNow.Date;
        var raw = await _context.AccAPBills.AsNoTracking()
            .Include(b => b.Vendor)
            .Where(b => b.Status != "Paid" && b.TotalAmount - b.RetentionAmount - b.PaidAmount > 0.01m)
            .OrderBy(b => b.Vendor!.VendorCode).ThenBy(b => b.DueDate)
            .ToListAsync();
        var rows = raw.Select(b => new AmsApAgingRowVm
        {
            APBillId = b.APBillID,
            BillNo = b.BillNo,
            VendorId = b.VendorID,
            VendorName = b.Vendor?.VendorName ?? "",
            DueDate = b.DueDate,
            Total = b.TotalAmount,
            Paid = b.PaidAmount,
            Retention = b.RetentionAmount,
            Balance = BillPayableBalance(b),
            DaysPastDue = (int)(today - b.DueDate).TotalDays
        }).ToList();
        return View("Aging", rows);
    }
}
