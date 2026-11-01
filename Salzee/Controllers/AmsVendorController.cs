using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsVendorController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsVendorController(PMSDbContext context, IModulePermissionService modulePermission)
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

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccVendors.AsNoTracking()
            .Include(v => v.AccountHead)
            .OrderBy(v => v.VendorCode)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
        return View(new AccVendor { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccVendor model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.VendorCode = model.VendorCode.Trim();
        model.VendorName = model.VendorName.Trim();
        if (await _context.AccVendors.AnyAsync(v => v.VendorCode == model.VendorCode))
            ModelState.AddModelError(nameof(model.VendorCode), "Vendor code already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
            return View(model);
        }

        model.VendorID = 0;
        model.CreatedAt = DateTime.UtcNow;
        model.CreatedBy = DbUserId10(CurrentUserId);
        _context.AccVendors.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Vendor saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var v = await _context.AccVendors.FirstOrDefaultAsync(x => x.VendorID == id);
        if (v == null) return NotFound();
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
        return View(v);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccVendor model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.VendorID) return BadRequest();

        model.VendorCode = model.VendorCode.Trim();
        model.VendorName = model.VendorName.Trim();
        if (await _context.AccVendors.AnyAsync(v => v.VendorCode == model.VendorCode && v.VendorID != id))
            ModelState.AddModelError(nameof(model.VendorCode), "Vendor code already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
            return View(model);
        }

        var v = await _context.AccVendors.FirstOrDefaultAsync(x => x.VendorID == id);
        if (v == null) return NotFound();
        v.VendorCode = model.VendorCode;
        v.VendorName = model.VendorName;
        v.VendorType = model.VendorType;
        v.NTN = model.NTN;
        v.STRN = model.STRN;
        v.ContactPerson = model.ContactPerson;
        v.Phone = model.Phone;
        v.Email = model.Email;
        v.Address = model.Address;
        v.BankAccountTitle = model.BankAccountTitle;
        v.BankAccountNumber = model.BankAccountNumber;
        v.BankName = model.BankName;
        v.IBAN = model.IBAN;
        v.AccountHeadID = model.AccountHeadID;
        v.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Vendor updated.";
        return RedirectToAction(nameof(Index));
    }
}
