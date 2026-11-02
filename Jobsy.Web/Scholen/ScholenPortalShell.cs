using System.Security.Claims;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

/// <summary>Resolved school / teacher portal chrome for SSR pages (mirrors <see cref="Components.Layout.SchoolLayout"/>).</summary>
public sealed class ScholenPortalShell
{
    private static readonly Regex ClassIdInPath = new(
        @"^/leraar/klas/([0-9a-fA-F-]{36})",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public enum Kind { Unknown, SchoolAdmin, Teacher }

    public Kind ShellKind { get; init; }
    public string HomeHref { get; init; } = "/school";
    public string ScopeLabel { get; init; } = "";
    public string RoleLabel { get; init; } = "";
    public IReadOnlyList<ScholenNav.ScholenNavGroup> Groups { get; init; } = [];
    public IReadOnlyList<(string LabelKey, string Href)> BottomNav { get; init; } = [];
    public IReadOnlyList<(string LabelKey, string Href)> TeacherMoreItems { get; init; } = [];
    public Guid? CurrentClassId { get; init; }
    public bool FeatureDisabled { get; init; }
    public string CurrentPath { get; init; } = "/";

    public static async Task<ScholenPortalShell> BuildAsync(
        HttpContext http,
        CultureState culture,
        JobsyApiClient api,
        IFeatureFlags features,
        string? routePath = null)
    {
        culture.InitializeFromRequest(http);
        var path = PageSeoCatalog.Normalize(routePath ?? http.Request.Path.Value);
        var featureOff = !await SchoolsFeatureGate.IsEnabledAsync(features);
        var user = http.User;

        if (path.StartsWith("/leraar", StringComparison.OrdinalIgnoreCase)
            || (RoleClaimMatching.HasRole(user, JobsyRoles.Teacher)
                && !RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)))
        {
            return await BuildTeacherAsync(http, culture, api, path, featureOff);
        }

        if (path.StartsWith("/school", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSchoolAdmin(culture, user, path, featureOff);
        }

        return new ScholenPortalShell
        {
            ShellKind = Kind.Unknown,
            CurrentPath = path,
            FeatureDisabled = featureOff,
        };
    }

    private static ScholenPortalShell BuildSchoolAdmin(
        CultureState culture,
        ClaimsPrincipal user,
        string path,
        bool featureOff)
    {
        var scope = user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(scope))
        {
            scope = culture["School.RoleChip"];
        }

        return new ScholenPortalShell
        {
            ShellKind = Kind.SchoolAdmin,
            HomeHref = "/school",
            RoleLabel = culture["School.RoleChip"],
            ScopeLabel = scope,
            Groups = ScholenNav.SchoolGroups,
            BottomNav = ScholenNav.SchoolBottomNav,
            CurrentPath = path,
            FeatureDisabled = featureOff,
        };
    }

    private static async Task<ScholenPortalShell> BuildTeacherAsync(
        HttpContext http,
        CultureState culture,
        JobsyApiClient api,
        string path,
        bool featureOff)
    {
        IReadOnlyList<TeacherAssignedClassDto> assigned = [];
        if (!featureOff)
        {
            try
            {
                assigned = await api.GetTeacherClassesAsync(http.RequestAborted);
            }
            catch
            {
                assigned = [];
            }
        }

        Guid? classId = ExtractClassId(path);
        if (classId is null && assigned.Count > 0)
        {
            classId = assigned[0].Id;
        }
        IReadOnlyList<ScholenNav.ScholenNavGroup> groups;
        IReadOnlyList<(string LabelKey, string Href)> bottom;
        IReadOnlyList<(string LabelKey, string Href)> more = [];
        string scopeLabel;

        if (classId is Guid cid)
        {
            var pairs = assigned.Select(c => (c.Id, c.ClassName)).ToList();
            groups = ScholenNav.TeacherGroupsForClass(cid, pairs);
            bottom = ScholenNav.TeacherBottomNavForClass(cid);
            more =
            [
                ("Leraar.Nav.DreamJobs", $"/leraar/klas/{cid:D}/droombanen"),
                ("Leraar.Nav.TestWindow", $"/leraar/klas/{cid:D}/testvenster"),
                ("Leraar.Nav.Materials", $"/leraar/klas/{cid:D}/materiaal"),
            ];
            var cls = assigned.FirstOrDefault(c => c.Id == cid);
            scopeLabel = cls is null ? culture["Leraar.RoleChip"] : $"Klas {cls.ClassName}";
        }
        else
        {
            groups = ScholenNav.TeacherGroups;
            bottom = ScholenNav.TeacherBottomNav;
            scopeLabel = culture["Leraar.RoleChip"];
        }

        return new ScholenPortalShell
        {
            ShellKind = Kind.Teacher,
            HomeHref = "/leraar",
            RoleLabel = culture["Leraar.RoleChip"],
            ScopeLabel = scopeLabel,
            Groups = groups,
            BottomNav = bottom,
            TeacherMoreItems = more,
            CurrentClassId = classId,
            CurrentPath = path,
            FeatureDisabled = featureOff,
        };
    }

    public static Guid? ExtractClassId(string path)
    {
        var m = ClassIdInPath.Match(path);
        return m.Success && Guid.TryParse(m.Groups[1].Value, out var id) ? id : null;
    }
}
