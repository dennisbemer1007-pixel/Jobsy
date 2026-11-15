using Jobsy.Core.Authorization;
using Jobsy.Web.Hosting;
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

        group.MapGet("", () => Page("04-team.html"));
        group.MapGet("/teams/{teamId:guid}", () => Page("04-team.html"));
        group.MapGet("/codes/{codeId:guid}/vragen", () => Page("05-vragen.html"));
        group.MapGet("/codes/{codeId:guid}/vergelijking", () => Page("06-vergelijking.html"));
        group.MapGet("/teams/{teamId:guid}/groep", () => Page("07-groepsbeeld.html"));
        return app;
    }

    private static IResult Page(string template)
    {
        var html = WerkgeverApkSsrHtml.RenderTemplate(template, new Dictionary<string, string>());
        return Results.Content(html, "text/html; charset=utf-8");
    }
}