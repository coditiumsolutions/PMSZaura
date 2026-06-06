using PMS.ViewModels;

namespace PMS.Services;

public interface INavigationService
{
    IReadOnlyList<NavigationModuleViewModel> GetModules();
    NavigationModuleViewModel? GetModule(string moduleKey);
    NavigationMenuItemViewModel? GetFirstNavigableItem(string moduleKey);
    string? ResolveModuleKeyFromRoute(string? controller, string? action);
    string GetActiveModuleKey(HttpContext httpContext, string? controller, string? action);
    bool TrySetActiveModule(HttpContext httpContext, string moduleKey);
    NavigationShellViewModel BuildShellViewModel(HttpContext httpContext, string? controller, string? action, bool showModuleNavigation);
    bool IsMenuItemActive(NavigationMenuItemViewModel item, string? controller, string? action, HttpContext? httpContext = null);
}
