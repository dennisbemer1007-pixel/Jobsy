using Jobsy.Web.Discovery;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

public static class CandidateDiscoveryBffEndpoints
{
    public const string NextPath = "/bff/candidate/discovery/next";

    public static IEndpointRouteBuilder MapCandidateDiscoveryBffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(NextPath, async (
            HttpContext http,
            IAntiforgery antiforgery,
            DiscoveryJourneyAdvanceService advance) =>
        {
            var wantsJson = CandidateBffHttp.WantsJson(http);
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Ongeldige sessie.")
                    : Results.BadRequest();
            }

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            if (!int.TryParse(form["step"].ToString(), out var step))
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Stap ontbreekt.")
                    : Results.BadRequest();
            }

            var screen = form["screen"].ToString();
            var clientJson = form["client_json"].ToString();
            var (ok, redirect, error) = await advance.AdvanceNextAsync(step, screen, clientJson, http.RequestAborted);
            if (!ok)
            {
                var failUrl = $"/candidate/ontdekkingsreis?stap={Uri.EscapeDataString(screen)}&discoveryError=1";
                return wantsJson
                    ? CandidateBffHttp.JsonFail(error ?? "Volgende mislukt.")
                    : Results.Redirect(failUrl);
            }

            var target = redirect ?? "/candidate/ontdekkingsreis";
            return wantsJson ? CandidateBffHttp.JsonOk(target) : Results.Redirect(target);
        }).RequireAuthorization().RequireRateLimiting("auth").WithOrder(-1000);

        return app;
    }
}
