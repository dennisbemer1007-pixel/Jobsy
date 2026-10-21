using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Web.Auth;
using Jobsy.Web.Models;
using Jobsy.Web.Profile;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

public static class CandidateProfileBffEndpoints
{
    public const string HttpClientName = "JobsyCandidateProfileBff";
    public const string PersonalPath = "/candidate/profile/sections/personal";
    public const string PreferencesPath = "/candidate/profile/sections/preferences";
    public const string AvailabilityPath = "/candidate/profile/sections/availability";
    public const string ExperiencePath = "/candidate/profile/sections/experience";

    public static IEndpointRouteBuilder MapCandidateProfileBffEndpoints(this IEndpointRouteBuilder app)
    {
        Map(app, PersonalPath, CandidateProfileFormApply.ApplyPersonal);
        Map(app, PreferencesPath, CandidateProfileFormApply.ApplyPreferences);
        Map(app, AvailabilityPath, CandidateProfileFormApply.ApplyAvailability);
        Map(app, ExperiencePath, CandidateProfileFormApply.ApplyExperience);
        return app;
    }

    private static void Map(
        IEndpointRouteBuilder app,
        string path,
        Func<IFormCollection, MeProfile, (bool Ok, string? ErrorKey)> apply)
    {
        app.MapPost(path, async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var section = CandidateProfileSectionUrls.SectionFromPath(path);
            var wantsJson = CandidateBffHttp.WantsJson(http);
            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Ongeldige sessie.")
                    : Results.Redirect(CandidateProfileSectionUrls.Page(section, error: true));
            }

            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Niet ingelogd.", status: 401)
                    : Results.Redirect(CandidateProfileSectionUrls.Page(section, error: true));
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var profile = await client.GetFromJsonAsync<MeProfile>("api/me/profile", http.RequestAborted);
            if (profile is null)
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Profiel kon niet worden geladen.")
                    : Results.Redirect(CandidateProfileSectionUrls.Page(section, error: true));
            }

            var (ok, _) = apply(form, profile);
            if (!ok)
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Controleer je invoer.")
                    : Results.Redirect(CandidateProfileSectionUrls.Page(section, error: true));
            }

            using var put = new HttpRequestMessage(HttpMethod.Put, "api/me/profile")
            {
                Content = JsonContent.Create(CandidateProfileFormApply.ToPutPayload(profile)),
            };
            using var resp = await client.SendAsync(put, http.RequestAborted);
            if (!resp.IsSuccessStatusCode)
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Opslaan is niet gelukt.")
                    : Results.Redirect(CandidateProfileSectionUrls.Page(section, error: true));
            }

            if (wantsJson)
            {
                return CandidateBffHttp.JsonOk();
            }

            return Results.Redirect(CandidateProfileSectionUrls.Page(section, saved: true));
        }).RequireAuthorization().WithOrder(-1000);
    }
}
