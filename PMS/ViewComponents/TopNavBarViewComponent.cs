using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Services;
using PMS.ViewModels;

namespace PMS.ViewComponents
{
    public class TopNavBarViewModel
    {
        public string UserDisplayName { get; set; } = string.Empty;
        public DateTime? LoginTime { get; set; }
        public NavigationShellViewModel? NavigationShell { get; set; }
    }

    public class TopNavBarViewComponent : ViewComponent
    {
        private readonly PMSDbContext _context;

        public TopNavBarViewComponent(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var controllerName = ViewContext.RouteData.Values["controller"]?.ToString();
            var actionName = ViewContext.RouteData.Values["action"]?.ToString();
            var showModuleNavigation = User.Identity?.IsAuthenticated == true
                && !IsAmsShell(controllerName);

            var model = new TopNavBarViewModel
            {
                UserDisplayName = User.Identity?.Name ?? "User"
            };

            if (showModuleNavigation)
            {
                var navigationService = HttpContext.RequestServices.GetService<INavigationService>();
                if (navigationService != null)
                {
                    model.NavigationShell = navigationService.BuildShellViewModel(
                        HttpContext,
                        controllerName,
                        actionName,
                        showModuleNavigation: true);
                }
            }

            var sessionId = UserClaimsPrincipal.FindFirst("SessionID")?.Value;
            if (!string.IsNullOrEmpty(sessionId))
            {
                var session = await _context.UserSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionID == sessionId);

                if (session != null)
                    model.LoginTime = session.LoginTime;
            }

            return View(model);
        }

        private static bool IsAmsShell(string? controllerName) =>
            !string.IsNullOrEmpty(controllerName)
            && (string.Equals(controllerName, "AccountsManagement", StringComparison.OrdinalIgnoreCase)
                || controllerName.StartsWith("Ams", StringComparison.OrdinalIgnoreCase));
    }
}
