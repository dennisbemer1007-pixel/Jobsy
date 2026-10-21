using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jobsy.Core;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

public static class CandidateSettingsBffEndpoints
{
    public const string MailPath = "/bff/candidate/settings/mail";

    private static readonly string[] MailPreferenceKeys =
        ["PushBom", "VacancyEngagementReminder", "CompanyReEngagement", "ComebackReminder"];

    public static IEndpointRouteBuilder MapCandidateSettingsBffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(MailPath, async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var wantsJson = CandidateBffHttp.WantsJson(http);
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Niet ingelogd.", status: 401)
                    : Results.Unauthorized();
            }

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Ongeldige sessie.")
                    : Results.BadRequest();
            }

            var items = MailPreferenceKeys.Select(k => new
            {
                key = k,
                label = k,
                enabled = form.ContainsKey("pref_" + k),
            }).ToList();
            bool? reminderEmailsEnabled = form.ContainsKey("reminder_emails_shown")
                ? form.ContainsKey("reminder_emails")
                : null;

            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                configuration["ApiBaseUrl"] ?? configuration["JobsyApi:BaseUrl"],
                "http://localhost:5200/");
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(15);

            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/me/email-preferences")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { items, reminderEmailsEnabled }),
                    Encoding.UTF8,
                    "application/json"),
            };
            if (!string.IsNullOrWhiteSpace(jwt))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }

            var response = await client.SendAsync(request, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return wantsJson
                    ? CandidateBffHttp.JsonFail("Opslaan is niet gelukt.")
                    : Results.StatusCode((int)response.StatusCode);
            }

            if (form.ContainsKey("whatsapp_shown"))
            {
                var optedIn = form.ContainsKey("whatsapp_optin");
                var phone = form["whatsapp_phone"].ToString();
                using var whatsApp = new HttpRequestMessage(HttpMethod.Put, "api/me/reminder-whatsapp")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { optedIn, phone }),
                        Encoding.UTF8,
                        "application/json"),
                };
                if (!string.IsNullOrWhiteSpace(jwt))
                {
                    whatsApp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                }

                var whatsAppResponse = await client.SendAsync(whatsApp, http.RequestAborted);
                if (!whatsAppResponse.IsSuccessStatusCode)
                {
                    return wantsJson
                        ? CandidateBffHttp.JsonFail("Opslaan is niet gelukt.")
                        : Results.StatusCode((int)whatsAppResponse.StatusCode);
                }
            }

            return wantsJson ? CandidateBffHttp.JsonOk() : Results.Ok();
        }).RequireAuthorization().WithOrder(-1000);

        return app;
    }
}
