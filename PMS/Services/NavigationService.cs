using PMS.ViewModels;

namespace PMS.Services;

public sealed class NavigationService : INavigationService
{
    public const string SessionKey = "PMS.ActiveNavModule";

    private static readonly IReadOnlyList<NavigationModuleViewModel> Modules = new[]
    {
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.Customer,
            Label = "Customer",
            IconClass = "fas fa-users",
            Items = new[]
            {
                new NavigationMenuItemViewModel
                {
                    Label = "Customers",
                    Controller = "Customer",
                    Action = "Index",
                    IconClass = "fas fa-user-friends me-2",
                    ExcludeActiveAction = "ByProject"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Registration",
                    Controller = "Registration",
                    Action = "Index",
                    IconClass = "fas fa-file-alt me-2"
                }
            }
        },
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.Operations,
            Label = "Operations",
            IconClass = "fas fa-cogs",
            Items = new[]
            {
                new NavigationMenuItemViewModel { Label = "Transfer", Controller = "Transfer", Action = "Index", IconClass = "fas fa-exchange-alt me-2" },
                new NavigationMenuItemViewModel { Label = "Transfer Fee", Controller = "TransferFee", Action = "Index", IconClass = "fas fa-tags me-2" },
                new NavigationMenuItemViewModel { Label = "NDC", Controller = "NDC", Action = "Index", IconClass = "fas fa-certificate me-2" },
                new NavigationMenuItemViewModel { Label = "Refund", Controller = "Refund", Action = "Index", IconClass = "fas fa-undo-alt me-2" },
                new NavigationMenuItemViewModel { Label = "Possession", Controller = "Possession", Action = "Index", IconClass = "fas fa-key me-2" },
                new NavigationMenuItemViewModel { Label = "Allotment", Controller = "Allotment", Action = "Index", IconClass = "fas fa-hand-holding me-2" },
                new NavigationMenuItemViewModel { Label = "Duplicate File", Controller = "DuplicateFileTransfer", Action = "Index", IconClass = "fas fa-copy me-2" },
                new NavigationMenuItemViewModel { Label = "Waiver", Controller = "Waiver", Action = "Index", IconClass = "fas fa-hand-holding-usd me-2" },
                new NavigationMenuItemViewModel { Label = "Rental", Controller = "Rental", Action = "Index", IconClass = "fas fa-key me-2" }
            }
        },
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.Property,
            Label = "Property",
            IconClass = "fas fa-building",
            Items = new[]
            {
                new NavigationMenuItemViewModel
                {
                    Label = "Properties",
                    Controller = "Property",
                    Action = "Index",
                    IconClass = "fas fa-home me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Floor Plan",
                    Controller = "Project",
                    Action = "FloorPlan",
                    IconClass = "fas fa-layer-group me-2",
                    ActiveAction = "FloorPlan"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Projects",
                    Controller = "Project",
                    Action = "Index",
                    IconClass = "fas fa-building me-2",
                    ExcludeActiveAction = "FloorPlan"
                }
            }
        },
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.Payments,
            Label = "Payments",
            IconClass = "fas fa-money-check-alt",
            Items = new[]
            {
                new NavigationMenuItemViewModel
                {
                    Label = "Paid Payments",
                    Controller = "Payment",
                    Action = "CustomerPayments",
                    IconClass = "fas fa-money-bill-wave me-2",
                    ActiveAction = "CustomerPayments"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Payment Plans",
                    Controller = "Payment",
                    Action = "PaymentPlans",
                    IconClass = "fas fa-file-invoice-dollar me-2",
                    ActiveActions = new[] { "PaymentPlans", "CreatePaymentPlan" }
                }
            }
        },
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.Sales,
            Label = "Sales",
            IconClass = "fas fa-chart-line",
            Items = new[]
            {
                new NavigationMenuItemViewModel
                {
                    Label = "Sales Inquiry",
                    Controller = "SalesInquiry",
                    Action = "Index",
                    IconClass = "fas fa-envelope-open-text me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Ticket Management",
                    Controller = "Ticket",
                    Action = "Index",
                    IconClass = "fas fa-ticket-alt me-2"
                }
            }
        },
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.Reports,
            Label = "Reports",
            IconClass = "fas fa-chart-bar",
            Items = new[]
            {
                new NavigationMenuItemViewModel
                {
                    IsSectionHeader = true,
                    Label = "Customer Reports",
                    SectionHeaderClass = "mt-1",
                    SectionKey = "customer-reports"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Active Members",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "total-active-members",
                    RouteTitle = "Active Members",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "total-active-members",
                    IconClass = "fas fa-users me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Cancelled",
                    Controller = "Reports",
                    Action = "BlockedCustomers",
                    ActiveAction = "BlockedCustomers",
                    IconClass = "fas fa-user-slash me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Defaulters",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "defaulter-x-installments",
                    RouteTitle = "Defaulters",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "defaulter-x-installments",
                    IconClass = "fas fa-filter me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Paid Members",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "all-paid-customers",
                    RouteTitle = "Paid Members",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "all-paid-customers",
                    IconClass = "fas fa-check-circle me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Due Amount",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "inst-due-amount",
                    RouteTitle = "Due Amount",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "inst-due-amount",
                    IconClass = "fas fa-file-invoice-dollar me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Project Wise",
                    Controller = "Customer",
                    Action = "ByProject",
                    ActiveAction = "ByProject",
                    IconClass = "fas fa-project-diagram me-2"
                },
                new NavigationMenuItemViewModel
                {
                    IsSectionHeader = true,
                    Label = "Transfer Reports",
                    SectionHeaderClass = "mt-2",
                    SectionKey = "transfer-reports"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Transfer Report",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "total-transfers-report",
                    RouteTitle = "Transfer Report",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "total-transfers-report",
                    IconClass = "fas fa-exchange-alt me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Daily transfer",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "daily-transfer-report",
                    RouteTitle = "Daily transfer",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "daily-transfer-report",
                    IconClass = "fas fa-calendar-day me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Project Wise",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "project-wise-transfer",
                    RouteTitle = "Project Wise",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "project-wise-transfer",
                    IconClass = "fas fa-sitemap me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Amount Received",
                    Controller = "Reports",
                    Action = "RequestedReport",
                    RouteId = "transfer-amount-received",
                    RouteTitle = "Amount Received",
                    ActiveAction = "RequestedReport",
                    ActiveRouteId = "transfer-amount-received",
                    IconClass = "fas fa-money-check-alt me-2"
                }
            }
        },
        new NavigationModuleViewModel
        {
            Key = NavigationModuleKeys.OtherLinks,
            Label = "Workspace",
            IconClass = "fas fa-th-large",
            Items = new[]
            {
                new NavigationMenuItemViewModel
                {
                    Label = "Dealers",
                    Controller = "Dealer",
                    Action = "Index",
                    IconClass = "fas fa-store me-2"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Accounts Management",
                    Controller = "AccountsManagement",
                    Action = "Index",
                    IconClass = "fas fa-calculator me-2",
                    ActiveControllerPrefix = "Ams"
                },
                new NavigationMenuItemViewModel
                {
                    Label = "User Management",
                    Controller = "Account",
                    Action = "Users",
                    IconClass = "fas fa-user-cog me-2",
                    ActiveAction = "Users",
                    RequiresAdminRole = true
                },
                new NavigationMenuItemViewModel
                {
                    Label = "Configuration",
                    Controller = "Settings",
                    Action = "Index",
                    IconClass = "fas fa-cogs me-2",
                    RequiresAdminRole = true
                }
            }
        }
    };

    private static readonly Dictionary<string, string> ControllerToModule = BuildControllerMap();

    public IReadOnlyList<NavigationModuleViewModel> GetModules() => Modules;

    public NavigationModuleViewModel? GetModule(string moduleKey) =>
        Modules.FirstOrDefault(m => string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase));

    public NavigationMenuItemViewModel? GetFirstNavigableItem(string moduleKey) =>
        GetModule(moduleKey)?.Items.FirstOrDefault(i => !i.IsSectionHeader);

    public string? ResolveModuleKeyFromRoute(string? controller, string? action)
    {
        if (string.IsNullOrEmpty(controller))
            return null;

        if (string.Equals(controller, "Customer", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "ByProject", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationModuleKeys.Reports;
        }

        if (string.Equals(controller, "Payment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(controller, "PaymentAudit", StringComparison.OrdinalIgnoreCase))
            return NavigationModuleKeys.Payments;

        if (string.Equals(controller, "SalesInquiry", StringComparison.OrdinalIgnoreCase)
            || string.Equals(controller, "Ticket", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationModuleKeys.Sales;
        }

        if (string.Equals(controller, "Reports", StringComparison.OrdinalIgnoreCase))
            return NavigationModuleKeys.Reports;

        if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "Workspace", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationModuleKeys.OtherLinks;
        }

        if (string.Equals(controller, "Settings", StringComparison.OrdinalIgnoreCase))
            return NavigationModuleKeys.OtherLinks;

        if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "Users", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationModuleKeys.OtherLinks;
        }

        return ControllerToModule.TryGetValue(controller, out var moduleKey) ? moduleKey : null;
    }

    public string GetActiveModuleKey(HttpContext httpContext, string? controller, string? action)
    {
        var routeModule = ResolveModuleKeyFromRoute(controller, action);
        if (!string.IsNullOrEmpty(routeModule))
        {
            httpContext.Session.SetString(SessionKey, routeModule);
            return routeModule;
        }

        var sessionModule = httpContext.Session.GetString(SessionKey);
        if (!string.IsNullOrEmpty(sessionModule) && GetModule(sessionModule) != null)
            return sessionModule;

        return NavigationModuleKeys.Customer;
    }

    public bool TrySetActiveModule(HttpContext httpContext, string moduleKey)
    {
        var module = GetModule(moduleKey);
        if (module == null)
            return false;

        httpContext.Session.SetString(SessionKey, module.Key);
        return true;
    }

    public NavigationShellViewModel BuildShellViewModel(HttpContext httpContext, string? controller, string? action, bool showModuleNavigation)
    {
        if (!showModuleNavigation)
        {
            return new NavigationShellViewModel
            {
                ShowModuleNavigation = false,
                Modules = Modules
            };
        }

        var activeKey = GetActiveModuleKey(httpContext, controller, action);
        return new NavigationShellViewModel
        {
            ActiveModuleKey = activeKey,
            ActiveModule = GetModule(activeKey),
            Modules = Modules,
            ShowModuleNavigation = true
        };
    }

    public bool IsMenuItemActive(NavigationMenuItemViewModel item, string? controller, string? action, HttpContext? httpContext = null)
    {
        if (item.IsSectionHeader)
            return false;

        var controllerMatches = !string.IsNullOrEmpty(item.ActiveControllerPrefix)
            ? string.Equals(controller, item.Controller, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(controller)
                    && controller.StartsWith(item.ActiveControllerPrefix, StringComparison.OrdinalIgnoreCase))
            : string.Equals(controller, item.Controller, StringComparison.OrdinalIgnoreCase);

        if (!controllerMatches)
            return false;

        if (!string.IsNullOrEmpty(item.ActiveRouteId))
        {
            var queryId = httpContext?.Request.Query["id"].ToString();
            var expectedAction = item.ActiveAction ?? item.Action;
            return string.Equals(action, expectedAction, StringComparison.OrdinalIgnoreCase)
                && string.Equals(queryId, item.ActiveRouteId, StringComparison.OrdinalIgnoreCase);
        }

        if (item.ActiveActions is { Length: > 0 })
        {
            return item.ActiveActions.Any(a => string.Equals(action, a, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(item.ActiveAction))
            return string.Equals(action, item.ActiveAction, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(item.ExcludeActiveAction))
            return !string.Equals(action, item.ExcludeActiveAction, StringComparison.OrdinalIgnoreCase);

        return true;
    }

    private static Dictionary<string, string> BuildControllerMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in Modules)
        {
            foreach (var item in module.Items)
            {
                if (item.IsSectionHeader)
                    continue;

                if (string.Equals(item.Controller, "Customer", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Action, "ByProject", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                map[item.Controller] = module.Key;
            }
        }

        return map;
    }
}
