using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsTaxTypeController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsTaxTypeController(PMSDbContext context, IModulePermissionService modulePermission)
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

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccTaxTypes.AsNoTracking()
            .Include(t => t.AccountHead)
            .OrderBy(t => t.TaxCategory).ThenBy(t => t.TaxCode)
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
        return View(new AccTaxType { IsActive = true, Rate = 0, TaxCategory = "WHT", AppliesTo = "Payable" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccTaxType model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        model.TaxCode = model.TaxCode.Trim();
        model.TaxName = model.TaxName.Trim();
        if (await _context.AccTaxTypes.AnyAsync(t => t.TaxCode == model.TaxCode))
            ModelState.AddModelError(nameof(model.TaxCode), "Tax code already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
            return View(model);
        }

        model.TaxTypeID = 0;
        _context.AccTaxTypes.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Tax type saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var t = await _context.AccTaxTypes.FirstOrDefaultAsync(x => x.TaxTypeID == id);
        if (t == null) return NotFound();
        ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
        return View(t);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccTaxType model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        if (id != model.TaxTypeID) return BadRequest();

        model.TaxCode = model.TaxCode.Trim();
        model.TaxName = model.TaxName.Trim();
        if (await _context.AccTaxTypes.AnyAsync(t => t.TaxCode == model.TaxCode && t.TaxTypeID != id))
            ModelState.AddModelError(nameof(model.TaxCode), "Tax code already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.AccountHeads = await _context.AccAccountHeads.AsNoTracking()
                .Where(h => h.IsActive && h.AllowDirectPosting).OrderBy(h => h.AccountCode).ToListAsync();
            return View(model);
        }

        var t = await _context.AccTaxTypes.FirstOrDefaultAsync(x => x.TaxTypeID == id);
        if (t == null) return NotFound();
        t.TaxCode = model.TaxCode;
        t.TaxName = model.TaxName;
        t.TaxCategory = model.TaxCategory;
        t.AppliesTo = model.AppliesTo;
        t.Rate = model.Rate;
        t.AccountHeadID = model.AccountHeadID;
        t.IsActive = model.IsActive;
        t.EffectiveFrom = model.EffectiveFrom;
        t.EffectiveTo = model.EffectiveTo;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Tax type updated.";
        return RedirectToAction(nameof(Index));
    }
}
