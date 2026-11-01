using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Services;

namespace Salzee.Controllers;

/// <summary>Shared AMS module permission checks (Read / Edit / Admin=Delete).</summary>
public abstract class AmsControllerBase : Controller
{
    public const string ModuleKey = "AccountsManagement";

    protected readonly PMSDbContext Context;
    protected readonly IModulePermissionService ModulePermission;
    protected readonly AmsAccessService AmsAccess;

    protected AmsControllerBase(
        PMSDbContext context,
        IModulePermissionService modulePermission,
        AmsAccessService amsAccess)
    {
        Context = context;
        ModulePermission = modulePermission;
        AmsAccess = amsAccess;
    }

    protected string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

    protected static string DbUserId10(string? userId) =>
        string.IsNullOrEmpty(userId) ? "SYSTEM" : userId.Length <= 10 ? userId : userId[..10];

    protected async Task<IActionResult?> EnsureAmsPermissionAsync(string requiredLevel)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var perm = await ModulePermission.GetPermissionAsync(userId, ModuleKey);
        if (requiredLevel == "Read" && !ModulePermission.CanRead(perm))
            return RedirectToAction("AccessDenied", "Account");
        if (requiredLevel == "Edit" && !ModulePermission.CanEdit(perm))
            return RedirectToAction("AccessDenied", "Account");
        if (requiredLevel == "Admin" && !await AmsAccess.IsAdminUserAsync(userId))
            return RedirectToAction("AccessDenied", "Account");
        ViewBag.CanCreate = ModulePermission.CanEdit(perm);
        ViewBag.CanEdit = ModulePermission.CanEdit(perm);
        ViewBag.CanDelete = await AmsAccess.IsAdminUserAsync(userId);
        return null;
    }

    protected IActionResult RedirectWithDeleteResult(AmsDeleteResult result, string successRedirectAction, object? routeValues = null)
    {
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;
        return RedirectToAction(successRedirectAction, routeValues);
    }
}
