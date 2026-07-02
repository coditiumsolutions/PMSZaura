namespace PMS.ViewModels;

public static class NavigationModuleKeys
{
    public const string Customer = "Customer";
    public const string Operations = "Operations";
    public const string Property = "Property";
    public const string Payments = "Payments";
    public const string Sales = "Sales";
    public const string Reports = "Reports";
    public const string OtherLinks = "OtherLinks";
}

public sealed class NavigationMenuItemViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Controller { get; init; } = string.Empty;
    public string Action { get; init; } = "Index";
    public string IconClass { get; init; } = string.Empty;
    public bool IsSectionHeader { get; init; }
    public string? SectionHeaderClass { get; init; }
    /// <summary>When set on a section header, child links collapse under this key until the next section header.</summary>
    public string? SectionKey { get; init; }
    public string? RouteId { get; init; }
    public string? RouteTitle { get; init; }
    /// <summary>When set, item is active only when current action equals this value.</summary>
    public string? ActiveAction { get; init; }
    /// <summary>When set, item is active only when current action does not equal this value.</summary>
    public string? ExcludeActiveAction { get; init; }
    /// <summary>When set, item is active when current action matches any of these values.</summary>
    public string[]? ActiveActions { get; init; }
    /// <summary>When set with RequestedReport, item is active when query id matches.</summary>
    public string? ActiveRouteId { get; init; }
    /// <summary>When true, item is shown only to users in the Admin role.</summary>
    public bool RequiresAdminRole { get; init; }
    /// <summary>When set, item is active when controller matches or starts with this prefix (e.g. Ams).</summary>
    public string? ActiveControllerPrefix { get; init; }
}

public sealed class NavigationModuleViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string IconClass { get; init; } = string.Empty;
    public IReadOnlyList<NavigationMenuItemViewModel> Items { get; init; } = Array.Empty<NavigationMenuItemViewModel>();
}

public sealed class NavigationShellViewModel
{
    public string ActiveModuleKey { get; init; } = NavigationModuleKeys.Customer;
    public NavigationModuleViewModel? ActiveModule { get; init; }
    public IReadOnlyList<NavigationModuleViewModel> Modules { get; init; } = Array.Empty<NavigationModuleViewModel>();
    public bool ShowModuleNavigation { get; init; }
}
