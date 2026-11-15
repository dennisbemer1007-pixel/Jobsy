using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkManagerEndpoints
{
    public static IEndpointRouteBuilder MapWerkgeverApkManagerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/werkgever/loopbaan-apk")
            .RequireAuthorization(policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(JobsyRoles.EnterpriseManager, JobsyRoles.Admin);
            });

        group.MapGet("", ListTeamsPage);
        group.MapGet("/teams/{teamId:guid}", TeamOverviewPage);
        group.MapGet("/codes/{codeId:guid}/vragen", ManagerVragenPage);
        group.MapGet("/codes/{codeId:guid}/vergelijking", () => Page("06-vergelijking.html", new Dictionary<string, string>()));
        group.MapGet("/teams/{teamId:guid}/groep", GroupInsightsPage);
        return app;
    }

    private static async Task<IResult> ListTeamsPage(HttpContext http, JobsyApiClient api, CancellationToken ct)
    {
        var teams = await api.GetWerkgeverApkTeamsAsync(ct);
        if (teams.Count > 0)
        {
            var first = teams[0];
            return Results.Redirect($"/werkgever/loopbaan-apk/teams/{first.Id:D}");
        }

        return Page("04-team.html", new Dictionary<string, string> { ["teamName"] = "Nog geen team" });
    }

    private static async Task<IResult> TeamOverviewPage(
        HttpContext http,
        JobsyApiClient api,
        Guid teamId,
        CancellationToken ct)
    {
        var overview = await api.GetWerkgeverApkTeamOverviewAsync(teamId, ct);
        if (overview is null)
        {
            return Results.Redirect("/werkgever/loopbaan-apk");
        }

        var shared = overview.Codes.Count(c => c.SharedWithManager);
        var busy = overview.Codes.Count(c =>
            c.Status == WerkgeverApkEmployeeCodeStatus.InProgress
            || (c.EmployeeCompleted && !c.SharedWithManager));
        var invited = overview.Codes.Count(c => c.Status == WerkgeverApkEmployeeCodeStatus.NotStarted);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["teamName"] = overview.TeamName,
            ["memberCount"] = overview.Codes.Count.ToString(),
            ["sharedCount"] = shared.ToString(),
            ["busyCount"] = busy.ToString(),
            ["invitedCount"] = invited.ToString(),
            ["groepVisible"] = shared >= WerkgeverApkAnonymity.MinGroupSize ? "true" : "false",
            ["groepMessage"] = shared >= WerkgeverApkAnonymity.MinGroupSize
                ? "Groepsbeeld is zichtbaar."
                : "Nog te weinig deelnemers, vanaf 5 zie je het groepsbeeld.",
        };
        return Page("04-team.html", tokens);
    }

    private static async Task<IResult> GroupInsightsPage(
        HttpContext http,
        JobsyApiClient api,
        Guid teamId,
        CancellationToken ct)
    {
        var insights = await api.GetWerkgeverApkGroupInsightsAsync(teamId, ct);
        var shared = insights?.SharedCompletedCount ?? 0;
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sharedCount"] = shared.ToString(),
            ["teamTitle"] = $"Team Logistiek · {shared} deelnemers",
            ["groepVisible"] = insights is { Visible: true } ? "true" : "false",
            ["groepMessage"] = insights is { Visible: false }
                ? $"Nog te weinig deelnemers ({insights.SharedCompletedCount}), vanaf {WerkgeverApkAnonymity.MinGroupSize} zie je het groepsbeeld."
                : "Anoniem groepsbeeld op basis van wie zelf deelde.",
        };
        return Page("07-groepsbeeld.html", tokens);
    }

    private static async Task<IResult> ManagerVragenPage(
        HttpContext http,
        JobsyApiClient api,
        Guid codeId,
        CancellationToken ct)
    {
        var sheet = await api.GetWerkgeverApkManagerSheetAsync(codeId, ct);
        var questions = await api.GetWerkgeverApkManagerQuestionsAsync(ct);
        if (sheet is null || questions.Count == 0)
        {
            return Results.Redirect("/werkgever/loopbaan-apk");
        }

        var displayIndex = questions.Count;
        for (var i = 0; i < questions.Count; i++)
        {
            if (!sheet.Answers.ContainsKey(questions[i].Id))
            {
                displayIndex = i + 1;
                break;
            }
        }
        var html = WerkgeverApkMockupPages.ManagerVragen(sheet, questions, displayIndex);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult Page(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate(template, tokens);
        return Results.Content(html, "text/html; charset=utf-8");
    }
}
