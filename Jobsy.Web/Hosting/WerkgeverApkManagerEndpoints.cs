using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
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
        group.MapGet("/teams/{teamId:guid}/codes/{codeId:guid}/functie", FunctieKiezenPage);
        group.MapPost("/teams/{teamId:guid}/codes/{codeId:guid}/functie", FunctieKiezenPost);
        group.MapGet("/teams/{teamId:guid}/functie-totaal", FunctieTotaalPage);
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

    private static async Task<IResult> FunctieKiezenPage(
        HttpContext http,
        JobsyApiClient api,
        Guid teamId,
        Guid codeId,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        var q = http.Request.Query["q"].ToString();
        var pick = await api.GetWerkgeverApkFunctiePickAsync(teamId, codeId, string.IsNullOrWhiteSpace(q) ? null : q, ct);
        if (pick is null)
        {
            return Results.Redirect($"/werkgever/loopbaan-apk/teams/{teamId:D}");
        }

        var teams = await api.GetWerkgeverApkTeamsAsync(ct);
        var team = teams.FirstOrDefault(t => t.Id == teamId);
        var tokens = antiforgery.GetAndStoreTokens(http);
        var af = $"""<input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">""";
        var searchPath = $"/werkgever/loopbaan-apk/teams/{teamId:D}/codes/{codeId:D}/functie";
        var postPath = searchPath;
        var html = WerkgeverApkFunctieMockupPages.FunctieKiezen(
            pick,
            team?.Name ?? "Team",
            "Demo Logistiek B.V.",
            postPath,
            searchPath,
            af);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> FunctieKiezenPost(
        HttpContext http,
        JobsyApiClient api,
        Guid teamId,
        Guid codeId,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        if (!http.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Redirect($"/werkgever/loopbaan-apk/teams/{teamId:D}/codes/{codeId:D}/functie?error=save");
        }

        var esco = http.Request.Form["escoOccupationId"].ToString();
        if (!string.IsNullOrWhiteSpace(esco))
        {
            await api.SetWerkgeverApkOccupationAsync(codeId, esco, ct);
        }

        var q = http.Request.Form["q"].ToString();
        var redirect = $"/werkgever/loopbaan-apk/teams/{teamId:D}/codes/{codeId:D}/functie";
        if (!string.IsNullOrWhiteSpace(q))
        {
            redirect += $"?q={Uri.EscapeDataString(q)}";
        }

        return Results.Redirect(redirect);
    }

    private static async Task<IResult> FunctieTotaalPage(
        HttpContext http,
        JobsyApiClient api,
        Guid teamId,
        CancellationToken ct)
    {
        var esco = http.Request.Query["esco"].ToString();
        var insights = await api.GetWerkgeverApkFunctieInsightsAsync(
            teamId,
            string.IsNullOrWhiteSpace(esco) ? null : esco,
            ct);
        if (insights is null)
        {
            return Results.Redirect($"/werkgever/loopbaan-apk/teams/{teamId:D}");
        }

        string html;
        if (insights.Visible)
        {
            html = WerkgeverApkFunctieMockupPages.Totaalbeeld(insights, "Demo Logistiek B.V.", "Team Logistiek");
        }
        else
        {
            html = WerkgeverApkFunctieMockupPages.TeWeinig(insights, "Demo Logistiek B.V.", "Team Logistiek", teamId);
        }

        return Results.Content(html, "text/html; charset=utf-8");
    }
}
