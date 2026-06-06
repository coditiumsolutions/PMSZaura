using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PMS.Services;
using PMS.ViewModels;

namespace PMS.Filters;

/// <summary>Keeps the selected PMS navigation module in session based on route or explicit selection.</summary>
public sealed class NavigationModuleFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true
            && context.Controller is Controller controller
            && ShouldApplyPmsModuleNavigation(context.RouteData.Values["controller"]?.ToString()))
        {
            var navigationService = context.HttpContext.RequestServices.GetRequiredService<INavigationService>();
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();
            var shell = navigationService.BuildShellViewModel(context.HttpContext, controllerName, actionName, showModuleNavigation: true);

            controller.ViewData["NavigationShell"] = shell;
            controller.ViewData["ActiveNavModuleKey"] = shell.ActiveModuleKey;
        }

        await next();
    }

    private static bool ShouldApplyPmsModuleNavigation(string? controllerName)
    {
        if (string.IsNullOrEmpty(controllerName))
            return false;

        if (string.Equals(controllerName, "AccountsManagement", StringComparison.OrdinalIgnoreCase)
            || controllerName.StartsWith("Ams", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
