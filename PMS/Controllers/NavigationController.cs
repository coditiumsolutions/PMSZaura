using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Services;

namespace PMS.Controllers;

[Authorize]
public sealed class NavigationController : Controller
{
    private readonly INavigationService _navigationService;

    public NavigationController(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [HttpGet]
    public IActionResult SelectModule(string module)
    {
        if (!_navigationService.TrySetActiveModule(HttpContext, module))
            return RedirectToAction("Index", "Home");

        var firstItem = _navigationService.GetFirstNavigableItem(module);
        if (firstItem == null)
            return RedirectToAction("Index", "Home");

        if (!string.IsNullOrEmpty(firstItem.RouteId))
        {
            return RedirectToAction(firstItem.Action, firstItem.Controller, new
            {
                id = firstItem.RouteId,
                title = firstItem.RouteTitle
            });
        }

        return RedirectToAction(firstItem.Action, firstItem.Controller);
    }
}
