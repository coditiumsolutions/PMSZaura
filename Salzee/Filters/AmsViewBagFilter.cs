using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Salzee.Services;

namespace Salzee.Filters;

/// <summary>Sets <c>ViewBag.CanDelete</c> on all AMS controllers when the logged-in user is UserType Admin.</summary>
public sealed class AmsViewBagFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller && IsAmsController(context.RouteData.Values["controller"]?.ToString()))
        {
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var access = context.HttpContext.RequestServices.GetRequiredService<AmsAccessService>();
            controller.ViewBag.CanDelete = await access.IsAdminUserAsync(userId, context.HttpContext.RequestAborted);
        }

        await next();
    }

    private static bool IsAmsController(string? controllerName) =>
        !string.IsNullOrEmpty(controllerName)
        && (controllerName.StartsWith("Ams", StringComparison.OrdinalIgnoreCase)
            || string.Equals(controllerName, "AccountsManagement", StringComparison.OrdinalIgnoreCase));
}
