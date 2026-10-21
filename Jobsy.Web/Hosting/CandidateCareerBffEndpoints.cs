using System.Net.Http.Headers;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Web.Hosting;

public static class CandidateCareerBffEndpoints
{
    public const string HttpClientName = "JobsyCandidateCareerBff";

    public static IEndpointRouteBuilder MapCandidateCareerBffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff/candidate/career")
            .RequireAuthorization();

        group.MapPost("/steps/{stepKey}/complete", HandleStepMutationAsync(
            (client, key, jwt, ct) => client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(key)}/complete", null, ct)))
            .RequireRateLimiting("auth");

        group.MapPost("/steps/{stepKey}/undo", HandleStepMutationAsync(
            (client, key, jwt, ct) => client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(key)}/uncomplete", null, ct)))
            .RequireRateLimiting("auth");

        return app;
    }

    private static Func<HttpContext, string, IAntiforgery, IHttpClientFactory, JobsyAccessTokenIssuer, Task<IResult>> HandleStepMutationAsync(
        Func<HttpClient, string, string, CancellationToken, Task<HttpResponseMessage>> send)
    {
        return async (http, stepKey, antiforgery, httpClientFactory, accessTokens) =>
        {
            var wantsJson = CandidateBffHttp.WantsJson(http);
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return wantsJson ? CandidateBffHttp.JsonFail("Ongeldige sessie.") : Results.BadRequest();
            }

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            var returnUrl = AuthRedirects.SafeLocalUrl(form["return_url"].ToString()) ?? "/carriere";

            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Niet ingelogd.", status: 401)
                    : Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await send(client, stepKey, jwt, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Actie mislukt.")
                    : Results.Redirect(returnUrl.Contains('?', StringComparison.Ordinal)
                        ? returnUrl + "&careerError=1"
                        : returnUrl + "?careerError=1");
            }

            return wantsJson ? CandidateBffHttp.JsonOk(returnUrl) : Results.Redirect(returnUrl);
        };
    }
}
