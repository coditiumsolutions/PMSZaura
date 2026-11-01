using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;
namespace Salzee.Controllers;

[Authorize]
public class AmsCoaController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;
    private readonly IWebHostEnvironment _env;
    private readonly AmsCoaClearService _coaClear;

    public AmsCoaController(
        PMSDbContext context,
        IModulePermissionService modulePermission,
        IWebHostEnvironment env,
        AmsCoaClearService coaClear)
    {
        _context = context;
        _modulePermission = modulePermission;
        _env = env;
        _coaClear = coaClear;
    }

    private string CoaImportTemplatePath =>
        Path.Combine(_env.WebRootPath, "templates", "ams", "coa-import.csv");

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

    public async Task<IActionResult> Index(int? categoryId)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var categories = await _context.AccAccountCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        var heads = await _context.AccAccountHeads.AsNoTracking()
            .Include(h => h.Category)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();

        if (categoryId.HasValue)
            heads = heads.Where(h => h.AccountCategoryID == categoryId.Value).ToList();

        var byParent = heads.ToLookup(h => h.ParentAccountHeadID);
        var rows = new List<AmsCoaRowVm>();
        void Walk(int? parentId, int depth)
        {
            foreach (var h in byParent[parentId].OrderBy(x => x.AccountCode))
            {
                rows.Add(new AmsCoaRowVm(h, depth));
                Walk(h.AccountHeadID, depth + 1);
            }
        }
        Walk(null, 0);

        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        await LoadLookupsAsync();
        return View(new AccAccountHead { AccountLevel = 3, AllowDirectPosting = true, IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(nameof(AccAccountHead.AccountCategoryID), nameof(AccAccountHead.ParentAccountHeadID), nameof(AccAccountHead.AccountCode), nameof(AccAccountHead.AccountName), nameof(AccAccountHead.AccountLevel), nameof(AccAccountHead.IsControlAccount), nameof(AccAccountHead.AllowDirectPosting), nameof(AccAccountHead.OpeningBalance), nameof(AccAccountHead.OpeningBalanceDate), nameof(AccAccountHead.OpeningBalanceType), nameof(AccAccountHead.Description), nameof(AccAccountHead.IsActive))]
        AccAccountHead model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (model.ParentAccountHeadID is 0)
            model.ParentAccountHeadID = null;

        if (await _context.AccAccountHeads.AnyAsync(h => h.AccountCode == model.AccountCode))
            ModelState.AddModelError(nameof(model.AccountCode), "Account code already exists.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.CreatedBy = CurrentUserId;
        _context.AccAccountHeads.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Account head created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var head = await _context.AccAccountHeads.FirstOrDefaultAsync(h => h.AccountHeadID == id);
        if (head == null) return NotFound();
        await LoadLookupsAsync(id);
        return View(head);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind(nameof(AccAccountHead.AccountHeadID), nameof(AccAccountHead.AccountCategoryID), nameof(AccAccountHead.ParentAccountHeadID), nameof(AccAccountHead.AccountCode), nameof(AccAccountHead.AccountName), nameof(AccAccountHead.AccountLevel), nameof(AccAccountHead.IsControlAccount), nameof(AccAccountHead.AllowDirectPosting), nameof(AccAccountHead.OpeningBalance), nameof(AccAccountHead.OpeningBalanceDate), nameof(AccAccountHead.OpeningBalanceType), nameof(AccAccountHead.Description), nameof(AccAccountHead.IsActive))]
        AccAccountHead model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (id != model.AccountHeadID) return BadRequest();

        if (model.ParentAccountHeadID is 0)
            model.ParentAccountHeadID = null;

        var existing = await _context.AccAccountHeads.FirstOrDefaultAsync(h => h.AccountHeadID == id);
        if (existing == null) return NotFound();

        if (await _context.AccAccountHeads.AnyAsync(h => h.AccountCode == model.AccountCode && h.AccountHeadID != id))
            ModelState.AddModelError(nameof(model.AccountCode), "Account code already exists.");

        if (model.ParentAccountHeadID == id)
            ModelState.AddModelError(nameof(model.ParentAccountHeadID), "Cannot be own parent.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(id);
            return View(model);
        }

        existing.AccountCategoryID = model.AccountCategoryID;
        existing.ParentAccountHeadID = model.ParentAccountHeadID;
        existing.AccountCode = model.AccountCode;
        existing.AccountName = model.AccountName;
        existing.AccountLevel = model.AccountLevel;
        existing.IsControlAccount = model.IsControlAccount;
        existing.AllowDirectPosting = model.AllowDirectPosting;
        existing.OpeningBalance = model.OpeningBalance;
        existing.OpeningBalanceDate = model.OpeningBalanceDate;
        existing.OpeningBalanceType = string.IsNullOrWhiteSpace(model.OpeningBalanceType) ? null : model.OpeningBalanceType;
        existing.Description = model.Description;
        existing.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Account head updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var head = await _context.AccAccountHeads.Include(h => h.Children)
            .FirstOrDefaultAsync(h => h.AccountHeadID == id);
        if (head == null) return NotFound();

        if (head.Children.Any(c => c.IsActive))
        {
            TempData["Error"] = "Deactivate child accounts first.";
            return RedirectToAction(nameof(Index));
        }

        var taxCount = await CountTaxTypesForAccountAsync(id);
        if (taxCount > 0)
        {
            TempData["Error"] = "This head is linked to TaxType rows; deactivate only (cannot remove while referenced).";
            head.IsActive = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        head.IsActive = false;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Account deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Import()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose a CSV file.");
            return View();
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var header = await reader.ReadLineAsync();
        var format = CoaCsvImportParser.DetectFormat(header);
        if (format == CoaCsvImportParser.CoaCsvFormat.Unknown)
        {
            TempData["Error"] = "Unrecognized CSV. Use coa-import format (S No, COA Code, Narration, Level #) or AMS 7-column format with AccountCode.";
            return View();
        }

        var categoryIdByName = await _context.AccAccountCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.CategoryName, c => c.AccountCategoryID, StringComparer.OrdinalIgnoreCase);

        var parseResult = CoaCsvImportParser.ParseFile(reader, format, header, categoryIdByName);
        var parsed = parseResult.Rows;
        var errors = parseResult.Errors.ToList();
        var imported = 0;

        var codeToId = await _context.AccAccountHeads.AsNoTracking()
            .ToDictionaryAsync(h => h.AccountCode, h => h.AccountHeadID, StringComparer.OrdinalIgnoreCase);

        foreach (var item in parsed)
        {
            if (await _context.AccAccountHeads.AnyAsync(h => h.AccountCode == item.AccountCode))
            {
                errors.Add($"Line {item.SourceLineNo}: code '{item.AccountCode}' already exists — skipped.");
                continue;
            }

            int? parentId = null;
            if (!string.IsNullOrEmpty(item.ParentAccountCode))
            {
                if (!codeToId.TryGetValue(item.ParentAccountCode, out var pid))
                {
                    errors.Add($"Line {item.SourceLineNo}: parent '{item.ParentAccountCode}' not found.");
                    continue;
                }
                parentId = pid;
            }

            var row = new AccAccountHead
            {
                AccountCategoryID = item.AccountCategoryId,
                ParentAccountHeadID = parentId,
                AccountCode = item.AccountCode,
                AccountName = item.AccountName,
                AccountLevel = item.AccountLevel,
                IsControlAccount = item.IsControlAccount,
                AllowDirectPosting = item.AllowDirectPosting,
                OpeningBalance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };
            _context.AccAccountHeads.Add(row);
            await _context.SaveChangesAsync();
            codeToId[item.AccountCode] = row.AccountHeadID;
            imported++;
        }

        if (errors.Count > 0)
            TempData["Error"] = string.Join(" ", errors.Take(12));
        TempData["Success"] = $"Imported {imported} account(s).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        var path = CoaImportTemplatePath;
        if (!System.IO.File.Exists(path))
        {
            TempData["Error"] = "COA import template file is missing on the server.";
            return RedirectToAction(nameof(Import));
        }

        return PhysicalFile(path, "text/csv", "coa-import.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ClearAll()
    {
        var denied = await EnsurePermissionAsync("Admin");
        if (denied != null) return denied;

        ViewBag.AccountCount = await _context.AccAccountHeads.CountAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearAll(string confirmText)
    {
        var denied = await EnsurePermissionAsync("Admin");
        if (denied != null) return denied;

        if (!string.Equals(confirmText?.Trim(), "DELETE COA", StringComparison.Ordinal))
        {
            TempData["Error"] = "Type DELETE COA exactly to confirm.";
            return RedirectToAction(nameof(ClearAll));
        }

        var result = await _coaClear.TryClearAllForReimportAsync();
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync(int? excludeHeadId = null)
    {
        ViewBag.Categories = await _context.AccAccountCategories.AsNoTracking()
            .Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
        var q = _context.AccAccountHeads.AsNoTracking().Where(h => h.IsActive).OrderBy(h => h.AccountCode);
        ViewBag.ParentHeads = excludeHeadId.HasValue
            ? await q.Where(h => h.AccountHeadID != excludeHeadId.Value).ToListAsync()
            : await q.ToListAsync();
    }

    private Task<int> CountTaxTypesForAccountAsync(int accountHeadId) =>
        _context.AccTaxTypes.CountAsync(t => t.AccountHeadID == accountHeadId);

    public sealed class AmsCoaRowVm
    {
        public AmsCoaRowVm(AccAccountHead head, int depth)
        {
            Head = head;
            Depth = depth;
        }
        public AccAccountHead Head { get; }
        public int Depth { get; }
    }
}
