using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;
using Salzee.Services;

namespace Salzee.Controllers;

[Authorize]
public class AmsBankController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly PMSDbContext _context;
    private readonly IModulePermissionService _modulePermission;

    public AmsBankController(PMSDbContext context, IModulePermissionService modulePermission)
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

    private async Task<AccBankAccount?> LoadBankAsync(int bankAccountId, bool track = false)
    {
        var q = _context.AccBankAccounts.AsQueryable();
        if (!track)
            q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(b => b.BankAccountID == bankAccountId);
    }

    private async Task<List<AccAccountHead>> LoadPostingHeadsAsync()
    {
        return await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && h.AllowDirectPosting)
            .OrderBy(h => h.AccountCode)
            .ToListAsync();
    }

    public async Task<IActionResult> Index()
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var list = await _context.AccBankAccounts.AsNoTracking()
            .Include(b => b.AccountHead)
            .OrderBy(b => b.BankName).ThenBy(b => b.AccountNumber)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        ViewBag.PostingHeads = await LoadPostingHeadsAsync();
        return View(new AccBankAccount
        {
            Currency = "PKR",
            IsActive = true,
            OpeningBalance = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(nameof(AccBankAccount.AccountHeadID), nameof(AccBankAccount.BankName), nameof(AccBankAccount.BranchName), nameof(AccBankAccount.BranchCode), nameof(AccBankAccount.AccountTitle), nameof(AccBankAccount.AccountNumber), nameof(AccBankAccount.IBAN), nameof(AccBankAccount.AccountType), nameof(AccBankAccount.Currency), nameof(AccBankAccount.OpeningBalance), nameof(AccBankAccount.OpeningDate), nameof(AccBankAccount.IsActive))]
        AccBankAccount model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var headOk = await _context.AccAccountHeads.AnyAsync(h =>
            h.AccountHeadID == model.AccountHeadID && h.IsActive && h.AllowDirectPosting);
        if (!headOk)
            ModelState.AddModelError(nameof(model.AccountHeadID), "Select an active posting ledger (GL).");

        if (!ModelState.IsValid)
        {
            ViewBag.PostingHeads = await LoadPostingHeadsAsync();
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.CreatedBy = CurrentUserId;
        _context.AccBankAccounts.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Bank account created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var row = await _context.AccBankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BankAccountID == id);
        if (row == null) return NotFound();

        ViewBag.PostingHeads = await LoadPostingHeadsAsync();
        return View(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(nameof(AccBankAccount.BankAccountID), nameof(AccBankAccount.AccountHeadID), nameof(AccBankAccount.BankName), nameof(AccBankAccount.BranchName), nameof(AccBankAccount.BranchCode), nameof(AccBankAccount.AccountTitle), nameof(AccBankAccount.AccountNumber), nameof(AccBankAccount.IBAN), nameof(AccBankAccount.AccountType), nameof(AccBankAccount.Currency), nameof(AccBankAccount.OpeningBalance), nameof(AccBankAccount.OpeningDate), nameof(AccBankAccount.IsActive))]
        AccBankAccount model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (id != model.BankAccountID) return BadRequest();

        var existing = await _context.AccBankAccounts.FirstOrDefaultAsync(b => b.BankAccountID == id);
        if (existing == null) return NotFound();

        var headOk = await _context.AccAccountHeads.AnyAsync(h =>
            h.AccountHeadID == model.AccountHeadID && h.IsActive && h.AllowDirectPosting);
        if (!headOk)
            ModelState.AddModelError(nameof(model.AccountHeadID), "Select an active posting ledger (GL).");

        if (!ModelState.IsValid)
        {
            ViewBag.PostingHeads = await LoadPostingHeadsAsync();
            return View(model);
        }

        existing.AccountHeadID = model.AccountHeadID;
        existing.BankName = model.BankName;
        existing.BranchName = model.BranchName;
        existing.BranchCode = model.BranchCode;
        existing.AccountTitle = model.AccountTitle;
        existing.AccountNumber = model.AccountNumber;
        existing.IBAN = model.IBAN;
        existing.AccountType = model.AccountType;
        existing.Currency = model.Currency;
        existing.OpeningBalance = model.OpeningBalance;
        existing.OpeningDate = model.OpeningDate;
        existing.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Bank account updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChequeBooks(int bankAccountId)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var bank = await LoadBankAsync(bankAccountId);
        if (bank == null) return NotFound();

        var books = await _context.AccChequeBooks.AsNoTracking()
            .Where(c => c.BankAccountID == bankAccountId)
            .OrderByDescending(c => c.IssuedDate)
            .ToListAsync();

        ViewBag.BankAccountId = bankAccountId;
        ViewBag.BankLabel = $"{bank.BankName} — {bank.AccountNumber}";
        return View(books);
    }

    [HttpGet]
    public async Task<IActionResult> ChequeBookCreate(int bankAccountId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var bank = await LoadBankAsync(bankAccountId);
        if (bank == null) return NotFound();

        ViewBag.BankAccountId = bankAccountId;
        ViewBag.BankLabel = $"{bank.BankName} — {bank.AccountNumber}";
        return View(new AccChequeBook
        {
            BankAccountID = bankAccountId,
            IssuedDate = DateTime.UtcNow.Date,
            IsActive = true,
            UsedLeaves = 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChequeBookCreate(
        int bankAccountId,
        [Bind(nameof(AccChequeBook.SeriesFrom), nameof(AccChequeBook.SeriesTo), nameof(AccChequeBook.TotalLeaves), nameof(AccChequeBook.UsedLeaves), nameof(AccChequeBook.IssuedDate), nameof(AccChequeBook.IsActive))]
        AccChequeBook model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var bank = await LoadBankAsync(bankAccountId);
        if (bank == null) return NotFound();

        model.BankAccountID = bankAccountId;
        if (model.TotalLeaves < 0)
            ModelState.AddModelError(nameof(model.TotalLeaves), "Total leaves cannot be negative.");
        if (model.UsedLeaves < 0)
            ModelState.AddModelError(nameof(model.UsedLeaves), "Used leaves cannot be negative.");
        if (model.UsedLeaves > model.TotalLeaves)
            ModelState.AddModelError(nameof(model.UsedLeaves), "Used leaves cannot exceed total leaves.");

        if (!ModelState.IsValid)
        {
            ViewBag.BankAccountId = bankAccountId;
            ViewBag.BankLabel = $"{bank.BankName} — {bank.AccountNumber}";
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        _context.AccChequeBooks.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cheque book added.";
        return RedirectToAction(nameof(ChequeBooks), new { bankAccountId });
    }

    [HttpGet]
    public async Task<IActionResult> ChequeBookEdit(int id)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var book = await _context.AccChequeBooks.AsNoTracking()
            .Include(c => c.BankAccount)
            .FirstOrDefaultAsync(c => c.ChequeBookID == id);
        if (book?.BankAccount == null) return NotFound();

        ViewBag.BankLabel = $"{book.BankAccount.BankName} — {book.BankAccount.AccountNumber}";
        return View(book);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChequeBookEdit(
        int id,
        [Bind(nameof(AccChequeBook.ChequeBookID), nameof(AccChequeBook.BankAccountID), nameof(AccChequeBook.SeriesFrom), nameof(AccChequeBook.SeriesTo), nameof(AccChequeBook.TotalLeaves), nameof(AccChequeBook.UsedLeaves), nameof(AccChequeBook.IssuedDate), nameof(AccChequeBook.IsActive))]
        AccChequeBook model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        if (id != model.ChequeBookID) return BadRequest();

        var existing = await _context.AccChequeBooks
            .Include(c => c.BankAccount)
            .FirstOrDefaultAsync(c => c.ChequeBookID == id);
        if (existing == null) return NotFound();

        if (model.TotalLeaves < 0)
            ModelState.AddModelError(nameof(model.TotalLeaves), "Total leaves cannot be negative.");
        if (model.UsedLeaves < 0)
            ModelState.AddModelError(nameof(model.UsedLeaves), "Used leaves cannot be negative.");
        if (model.UsedLeaves > model.TotalLeaves)
            ModelState.AddModelError(nameof(model.UsedLeaves), "Used leaves cannot exceed total leaves.");

        if (!ModelState.IsValid)
        {
            model.BankAccount = existing.BankAccount;
            ViewBag.BankLabel = $"{existing.BankAccount!.BankName} — {existing.BankAccount.AccountNumber}";
            return View(model);
        }

        existing.SeriesFrom = model.SeriesFrom;
        existing.SeriesTo = model.SeriesTo;
        existing.TotalLeaves = model.TotalLeaves;
        existing.UsedLeaves = model.UsedLeaves;
        existing.IssuedDate = model.IssuedDate;
        existing.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cheque book updated.";
        return RedirectToAction(nameof(ChequeBooks), new { bankAccountId = existing.BankAccountID });
    }

    public async Task<IActionResult> ChequeRegisters(int bankAccountId)
    {
        var denied = await EnsurePermissionAsync("Read");
        if (denied != null) return denied;

        var bank = await LoadBankAsync(bankAccountId);
        if (bank == null) return NotFound();

        var rows = await _context.AccChequeRegisters.AsNoTracking()
            .Where(r => r.BankAccountID == bankAccountId)
            .OrderByDescending(r => r.EntryDate).ThenByDescending(r => r.ChequeRegisterID)
            .ToListAsync();

        ViewBag.BankAccountId = bankAccountId;
        ViewBag.BankLabel = $"{bank.BankName} — {bank.AccountNumber}";
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> ChequeRegisterCreate(int bankAccountId)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var bank = await LoadBankAsync(bankAccountId);
        if (bank == null) return NotFound();

        ViewBag.BankAccountId = bankAccountId;
        ViewBag.BankLabel = $"{bank.BankName} — {bank.AccountNumber}";
        ViewBag.ChequeBooks = await _context.AccChequeBooks.AsNoTracking()
            .Where(c => c.BankAccountID == bankAccountId && c.IsActive)
            .OrderByDescending(c => c.IssuedDate)
            .ToListAsync();

        return View(new AccChequeRegister
        {
            BankAccountID = bankAccountId,
            ChequeDate = DateTime.UtcNow.Date,
            EntryDate = DateTime.UtcNow.Date,
            Status = "Pending",
            ChequeType = "Payment"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChequeRegisterCreate(
        int bankAccountId,
        [Bind(nameof(AccChequeRegister.ChequeBookID), nameof(AccChequeRegister.ChequeNo), nameof(AccChequeRegister.ChequeDate), nameof(AccChequeRegister.EntryDate), nameof(AccChequeRegister.IsPostDated), nameof(AccChequeRegister.ChequeType), nameof(AccChequeRegister.Amount), nameof(AccChequeRegister.PayableTo), nameof(AccChequeRegister.ReceivedFrom), nameof(AccChequeRegister.Remarks))]
        AccChequeRegister model)
    {
        var denied = await EnsurePermissionAsync("Edit");
        if (denied != null) return denied;

        var bank = await LoadBankAsync(bankAccountId);
        if (bank == null) return NotFound();

        model.BankAccountID = bankAccountId;
        model.Status = "Pending";
        if (model.ChequeBookID is 0)
            model.ChequeBookID = null;

        if (model.Amount <= 0)
            ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than zero.");

        if (model.ChequeBookID is int cbId)
        {
            var belongs = await _context.AccChequeBooks.AnyAsync(c =>
                c.ChequeBookID == cbId && c.BankAccountID == bankAccountId);
            if (!belongs)
                ModelState.AddModelError(nameof(model.ChequeBookID), "Cheque book does not belong to this bank account.");
        }
        else
            model.ChequeBookID = null;

        if (!ModelState.IsValid)
        {
            ViewBag.BankAccountId = bankAccountId;
            ViewBag.BankLabel = $"{bank.BankName} — {bank.AccountNumber}";
            ViewBag.ChequeBooks = await _context.AccChequeBooks.AsNoTracking()
                .Where(c => c.BankAccountID == bankAccountId && c.IsActive)
                .OrderByDescending(c => c.IssuedDate)
                .ToListAsync();
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.CreatedBy = CurrentUserId;
        _context.AccChequeRegisters.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cheque register row added (skeleton — no voucher link yet).";
        return RedirectToAction(nameof(ChequeRegisters), new { bankAccountId });
    }
}
