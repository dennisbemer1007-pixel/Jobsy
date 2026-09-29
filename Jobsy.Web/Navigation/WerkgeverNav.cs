using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Navigation;

public enum EmployerRole
{
    Bedrijfsmanager,
    Regiomanager,
    Vestigingsmanager,
    Intermediair
}

public enum RoleVisibilityKind
{
    Full,
    ReadOnly,
    OwnScope,
    Hidden
}

public sealed record RoleVisibility(
    RoleVisibilityKind Bedrijfsmanager,
    RoleVisibilityKind Regiomanager,
    RoleVisibilityKind Vestigingsmanager,
    RoleVisibilityKind Intermediair = RoleVisibilityKind.Hidden)
{
    public RoleVisibilityKind For(EmployerRole role) => role switch
    {
        EmployerRole.Bedrijfsmanager => Bedrijfsmanager,
        EmployerRole.Regiomanager => Regiomanager,
        EmployerRole.Vestigingsmanager => Vestigingsmanager,
        EmployerRole.Intermediair => Intermediair,
        _ => RoleVisibilityKind.Hidden
    };
}

public sealed record WerkgeverNavItem(
    string Key,
    string LabelKey,
    string Href,
    string Svg,
    IReadOnlyList<string> Aliases,
    RoleVisibility Visibility,
    string? AvailabilityKey = null,
    string? CountKey = null,
    IReadOnlyDictionary<EmployerRole, string>? LabelOverrides = null,
    bool IsAvailable = true)
{
    public string LabelKeyFor(EmployerRole role)
        => LabelOverrides is not null && LabelOverrides.TryGetValue(role, out var over)
            ? over
            : LabelKey;
}

public sealed record WerkgeverNavGroup(
    string Key,
    string LabelKey,
    IReadOnlyList<WerkgeverNavItem> Items,
    IReadOnlyDictionary<EmployerRole, string>? LabelOverrides = null)
{
    public string LabelKeyFor(EmployerRole role)
        => LabelOverrides is not null && LabelOverrides.TryGetValue(role, out var over)
            ? over
            : LabelKey;
}

/// <summary>
/// Flags that gate conditional nav items (§IA). Evaluated once; no scattered ifs in the catalog.
/// </summary>
public sealed record WerkgeverNavContext(
    bool HasApiOrCsvImport = false,
    bool HasTakeovers = false,
    bool HasSalesReferral = false,
    bool CandidateInsightsEnabled = true,
    bool HasCandidateApplications = false,
    bool HasMultipleBranches = false,
    IReadOnlyDictionary<string, int>? Counts = null);

/// <summary>
/// Single source of truth for employer sidebar, mobile bottom-nav, labels and crumbs (§IA).
/// </summary>
public static class WerkgeverNav
{
    private static readonly RoleVisibility BmRmVm =
        new(RoleVisibilityKind.Full, RoleVisibilityKind.ReadOnly, RoleVisibilityKind.OwnScope);

    private static readonly RoleVisibility BmOnly =
        new(RoleVisibilityKind.Full, RoleVisibilityKind.Hidden, RoleVisibilityKind.Hidden);

    private static readonly RoleVisibility BmVm =
        new(RoleVisibilityKind.Full, RoleVisibilityKind.Hidden, RoleVisibilityKind.OwnScope);

    private static readonly RoleVisibility BmRm =
        new(RoleVisibilityKind.Full, RoleVisibilityKind.ReadOnly, RoleVisibilityKind.Hidden);

    private static readonly RoleVisibility BmVmSalary =
        new(RoleVisibilityKind.Full, RoleVisibilityKind.Hidden, RoleVisibilityKind.ReadOnly);

    private static readonly RoleVisibility IntermediaryVisible =
        new(RoleVisibilityKind.Hidden, RoleVisibilityKind.Hidden, RoleVisibilityKind.Hidden, RoleVisibilityKind.Full);

    public static readonly IReadOnlyList<WerkgeverNavGroup> Catalog =
    [
        new("overview", "WgNav.Group.Overview",
        [
            new("dashboard", "WgNav.Dashboard", "/werkgever", NavIcons.Home,
                ["/home", "/branch", "/regional"], BmRmVm with { Intermediair = RoleVisibilityKind.Full }),
            new("todo", "WgNav.Todo", "/werkgever/te-doen", NavIcons.List,
                [], BmRmVm,
                LabelOverrides: new Dictionary<EmployerRole, string>
                {
                    [EmployerRole.Regiomanager] = "WgNav.Signals"
                },
                CountKey: "todo"),
        ]),
        new("recruitment", "WgNav.Group.Recruitment",
        [
            new("vacancies", "WgNav.Vacancies", "/werkgever/vacatures", NavIcons.Vacancies,
                ["/employer/vacancies", "/branch/vacancies"], BmRmVm with { Intermediair = RoleVisibilityKind.Full }),
            new("applications", "WgNav.Applications", "/werkgever/sollicitaties", NavIcons.Applications,
                ["/branch/applicants"], BmRmVm with { Intermediair = RoleVisibilityKind.Full },
                CountKey: "applications"),
            new("talent", "WgNav.Talentpool", "/werkgever/talentpool", NavIcons.Users,
                ["/employer/talent", "/employer/talent-contacts"], BmRmVm with { Intermediair = RoleVisibilityKind.Full }),
            new("insights", "WgNav.CandidateInsights", "/werkgever/kandidaatinzichten", NavIcons.Users,
                ["/employer/kandidaatinzichten"], BmRmVm,
                AvailabilityKey: "insights"),
        ]),
        new("organisation", "WgNav.Group.Organisation",
        [
            new("branches", "WgNav.BranchesRegions", "/werkgever/organisatie/vestigingen", NavIcons.Branches,
                ["/employer/branches", "/regional/branches", "/employer/regions", "/employer/organization"], BmRm),
            new("team", "WgNav.TeamRights", "/werkgever/organisatie/team", NavIcons.Users,
                ["/employer/users"], BmOnly),
            new("profile", "WgNav.CompanyProfile", "/werkgever/organisatie/profiel", NavIcons.Companies,
                ["/employer/company", "/employer/culture", "/branch/culture"], BmVm,
                LabelOverrides: new Dictionary<EmployerRole, string>
                {
                    [EmployerRole.Vestigingsmanager] = "WgNav.BranchProfile"
                }),
            new("salary", "WgNav.SalaryTables", "/werkgever/organisatie/salaristabellen", NavIcons.Wages,
                ["/employer/salary-tables"], BmVmSalary),
        ],
        LabelOverrides: new Dictionary<EmployerRole, string>
        {
            [EmployerRole.Vestigingsmanager] = "WgNav.Group.MyBranch",
            [EmployerRole.Regiomanager] = "WgNav.Group.MyRegion"
        }),
        new("tokens", "WgNav.Group.Tokens",
        [
            new("balance", "WgNav.BalanceBuy", "/werkgever/tokens", NavIcons.Tokens,
                ["/employer/tokens", "/branch/tokens", "/regional/tokens"], BmRmVm with { Intermediair = RoleVisibilityKind.Full },
                LabelOverrides: new Dictionary<EmployerRole, string>
                {
                    [EmployerRole.Vestigingsmanager] = "WgNav.BalanceRequest",
                    [EmployerRole.Regiomanager] = "WgNav.TokenUsage"
                }),
            new("usage", "WgNav.UsagePerBranch", "/werkgever/tokens/verbruik", NavIcons.Tokens,
                [], BmRm, IsAvailable: false),
            new("mutations", "WgNav.Mutations", "/werkgever/tokens/mutaties", NavIcons.Logging,
                [], BmRmVm, IsAvailable: false),
            new("invoices", "WgNav.Invoices", "/werkgever/tokens/facturen", NavIcons.Finance,
                [], BmOnly, IsAvailable: false),
        ],
        LabelOverrides: new Dictionary<EmployerRole, string>
        {
            [EmployerRole.Regiomanager] = "WgNav.Group.TokenUsage"
        }),
        new("more", "WgNav.Group.More",
        [
            new("integrations", "WgNav.Integrations", "/werkgever/koppelingen", NavIcons.Api,
                ["/employer/csv-import"], BmOnly, AvailabilityKey: "integrations"),
            new("takeovers", "WgNav.Takeovers", "/werkgever/overnames", NavIcons.Branches,
                ["/employer/takeovers"], BmVm, AvailabilityKey: "takeovers"),
            new("materials", "WgNav.RecruitmentMaterials", "/werkgever/wervingsmateriaal", NavIcons.Shared,
                ["/employer/tokens?tab=tracking"], BmRmVm),
            new("partner", "WgNav.PartnerProgram", "/werkgever/partner", NavIcons.Finance,
                ["/employer/sales", "/employer/sales/payout-checkout"], BmOnly,
                AvailabilityKey: "partner"),
            // Intermediary-only clients/team keep their existing URLs (D1).
            new("clients", "WgNav.Clients", "/intermediary", NavIcons.Companies,
                [], IntermediaryVisible),
            new("intermediary-team", "WgNav.Team", "/intermediary/team", NavIcons.Users,
                [], IntermediaryVisible),
            new("my-applications", "WgNav.MyApplications", "/candidate/applications", NavIcons.Applications,
                [], new RoleVisibility(
                    RoleVisibilityKind.Full, RoleVisibilityKind.Full, RoleVisibilityKind.Full, RoleVisibilityKind.Full),
                AvailabilityKey: "my-applications"),
        ]),
    ];

    public static EmployerRole? ResolveRole(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.EnterpriseManager))
        {
            return EmployerRole.Bedrijfsmanager;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.RegionalManager))
        {
            return EmployerRole.Regiomanager;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.BranchManager))
        {
            return EmployerRole.Vestigingsmanager;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Intermediary))
        {
            return EmployerRole.Intermediair;
        }

        return null;
    }

    public static IReadOnlyList<WerkgeverNavGroup> For(EmployerRole role, WerkgeverNavContext? ctx = null)
    {
        ctx ??= new WerkgeverNavContext();
        var groups = new List<WerkgeverNavGroup>();

        foreach (var group in Catalog)
        {
            var items = group.Items
                .Where(i => IsItemVisible(i, role, ctx))
                .Select(i => i with { IsAvailable = ResolveAvailability(i, ctx) })
                .Where(i => i.IsAvailable)
                .ToList();

            if (items.Count == 0)
            {
                continue;
            }

            groups.Add(group with { Items = items });
        }

        return groups;
    }

    public static IReadOnlyList<WerkgeverNavItem> MobileItems(EmployerRole role, WerkgeverNavContext? ctx = null)
    {
        ctx ??= new WerkgeverNavContext();
        var all = For(role, ctx).SelectMany(g => g.Items).ToList();

        static WerkgeverNavItem? Find(IReadOnlyList<WerkgeverNavItem> items, string key)
            => items.FirstOrDefault(i => i.Key == key);

        var meer = new WerkgeverNavItem(
            "meer", "WgNav.More", "#meer", NavIcons.Menu, [],
            new RoleVisibility(RoleVisibilityKind.Full, RoleVisibilityKind.Full, RoleVisibilityKind.Full, RoleVisibilityKind.Full));

        return role switch
        {
            EmployerRole.Bedrijfsmanager or EmployerRole.Vestigingsmanager or EmployerRole.Intermediair =>
            [
                Find(all, "dashboard") ?? all[0],
                Find(all, "vacancies")!,
                Find(all, "applications") ?? Find(all, "talent")!,
                Find(all, "balance")!,
                meer
            ],
            EmployerRole.Regiomanager =>
            [
                Find(all, "dashboard")!,
                Find(all, "vacancies")!,
                Find(all, "applications")!,
                Find(all, "insights") ?? Find(all, "talent")!,
                meer
            ],
            _ => [meer]
        };
    }

    public static (WerkgeverNavGroup Group, WerkgeverNavItem Item)? Resolve(string relativePath)
    {
        var path = Normalize(relativePath);
        WerkgeverNavGroup? bestGroup = null;
        WerkgeverNavItem? bestItem = null;
        var bestScore = -1;

        foreach (var group in Catalog)
        {
            foreach (var item in group.Items)
            {
                var href = Normalize(item.Href);
                if (string.Equals(path, href, StringComparison.OrdinalIgnoreCase))
                {
                    return (group, item);
                }

                foreach (var alias in item.Aliases)
                {
                    if (string.Equals(path, Normalize(alias), StringComparison.OrdinalIgnoreCase))
                    {
                        return (group, item);
                    }
                }

                if (href is not "/" and not "#meer"
                    && path.StartsWith(href + "/", StringComparison.OrdinalIgnoreCase))
                {
                    var score = href.Length;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestGroup = group;
                        bestItem = item;
                    }
                }
            }
        }

        return bestGroup is null || bestItem is null ? null : (bestGroup, bestItem);
    }

    /// <summary>
    /// Breadcrumb trail: scope label › group › item [› detail]. Scope is supplied by the caller.
    /// </summary>
    public static IReadOnlyList<(string LabelKey, string? Href, bool IsCurrent)> Crumbs(
        string relativePath,
        EmployerRole role,
        string? detail = null)
    {
        var crumbs = new List<(string LabelKey, string? Href, bool IsCurrent)>();
        var resolved = Resolve(relativePath);
        if (resolved is null)
        {
            if (!string.IsNullOrWhiteSpace(detail))
            {
                crumbs.Add((detail, null, true));
            }

            return crumbs;
        }

        var (group, item) = resolved.Value;
        crumbs.Add((group.LabelKeyFor(role), null, false));
        var itemIsCurrent = string.IsNullOrWhiteSpace(detail);
        crumbs.Add((item.LabelKeyFor(role), itemIsCurrent ? null : item.Href, itemIsCurrent));
        if (!string.IsNullOrWhiteSpace(detail))
        {
            crumbs.Add((detail!, null, true));
        }

        return crumbs;
    }

    public static string Normalize(string relativePath)
    {
        var path = relativePath.Split('?', 2)[0].Split('#', 2)[0].Trim('/');
        return string.IsNullOrEmpty(path) ? "/" : "/" + path;
    }

    private static bool IsItemVisible(WerkgeverNavItem item, EmployerRole role, WerkgeverNavContext ctx)
    {
        if (item.Visibility.For(role) == RoleVisibilityKind.Hidden)
        {
            return false;
        }

        return ResolveAvailability(item, ctx);
    }

    private static bool ResolveAvailability(WerkgeverNavItem item, WerkgeverNavContext ctx)
    {
        if (!item.IsAvailable && item.AvailabilityKey is null)
        {
            // Page not built yet (IsAvailable: false without a conditional key).
            return false;
        }

        return item.AvailabilityKey switch
        {
            null => item.IsAvailable,
            "integrations" => ctx.HasApiOrCsvImport,
            "takeovers" => ctx.HasTakeovers,
            "partner" => ctx.HasSalesReferral,
            "insights" => ctx.CandidateInsightsEnabled,
            "my-applications" => ctx.HasCandidateApplications,
            "regions-tab" => ctx.HasMultipleBranches,
            _ => item.IsAvailable
        };
    }
}
