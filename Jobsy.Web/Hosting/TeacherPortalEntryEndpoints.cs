using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET /leraar — redirect to first class or empty state HTML without Blazor.</summary>
public static class TeacherPortalEntryEndpoints
{
    public const string PagePath = "/leraar";

    public static IEndpointRouteBuilder MapTeacherPortalEntryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PagePath, GetAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{JobsyRoles.Teacher},{JobsyRoles.SchoolAdmin}" });
        return app;
    }

    private static async Task<IResult> GetAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        if (!await SchoolsFeatureGate.IsEnabledAsync(features, cancellationToken))
        {
            return Results.Content(
                ScholenPortalSsrFrame.RenderDocument(
                    culture["Leraar.DashboardTitle"],
                    "",
                    new ScholenPortalShell
                    {
                        ShellKind = ScholenPortalShell.Kind.Teacher,
                        HomeHref = PagePath,
                        CurrentPath = PagePath,
                        FeatureDisabled = true,
                        RoleLabel = culture["Leraar.RoleChip"],
                        ScopeLabel = culture["Leraar.RoleChip"],
                    },
                    culture,
                    PagePath),
                "text/html; charset=utf-8");
        }

        var user = http.User;
        if (RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
            && !RoleClaimMatching.HasRole(user, JobsyRoles.Teacher))
        {
            return Results.Redirect("/school");
        }

        IReadOnlyList<TeacherAssignedClassDto> classes;
        try
        {
            classes = await api.GetTeacherClassesAsync(cancellationToken);
        }
        catch
        {
            return Results.Content(
                ScholenPortalSsrFrame.RenderDocument(
                    culture["Leraar.DashboardTitle"],
                    $"""<p class="state-message error">{System.Net.WebUtility.HtmlEncode(culture["Leraar.LoadFailed"])}</p>""",
                    await ScholenPortalShell.BuildAsync(http, culture, api, features, PagePath),
                    culture,
                    PagePath),
                "text/html; charset=utf-8");
        }

        if (classes.Count > 0)
        {
            return Results.Redirect($"/leraar/klas/{classes[0].Id:D}");
        }

        var body = $"""
            <header class="sch-page-header"><h1>{System.Net.WebUtility.HtmlEncode(culture["Leraar.DashboardTitle"])}</h1></header>
            <div class="sch-empty-state panel-section"><p>{System.Net.WebUtility.HtmlEncode(culture["Leraar.NoClass"])}</p></div>
            """;
        var shell = await ScholenPortalShell.BuildAsync(http, culture, api, features, PagePath);
        return Results.Content(
            ScholenPortalSsrFrame.RenderDocument(culture["Leraar.DashboardTitle"], body, shell, culture, PagePath),
            "text/html; charset=utf-8");
    }
}
