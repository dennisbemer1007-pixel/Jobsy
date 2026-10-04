using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Core;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Auth;

public static class MailSettingsEndpoints
{
    private static readonly string[] handler = new[] { "PushBom", "VacancyEngagementReminder", "CompanyReEngagement", "ComebackReminder" };

    public static IEndpointRouteBuilder MapMailSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/mail-instellingen", async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString("/account/mail-instellingen"));
            }

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/account/mail-instellingen");
            }

            var form = await http.Request.ReadFormAsync();
            var keys = handler;
            var items = keys.Select(k => new
            {
                key = k,
                label = k,
                enabled = form.ContainsKey("pref_" + k)
            }).ToList();

            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(
                configuration["ApiBaseUrl"] ?? configuration["JobsyApi:BaseUrl"],
                "http://localhost:5200/");
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(15);

            var jwt = accessTokens.TryCreate(http.User, http.Connection.RemoteIpAddress?.ToString());
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/me/email-preferences")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { items }),
                    Encoding.UTF8,
                    "application/json")
            };
            if (!string.IsNullOrWhiteSpace(jwt))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect("/account/mail-instellingen?error=1");
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
                        "application/json")
                };
                if (!string.IsNullOrWhiteSpace(jwt))
                {
                    whatsApp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                }

                var whatsAppResponse = await client.SendAsync(whatsApp);
                if (!whatsAppResponse.IsSuccessStatusCode)
                {
                    return Results.Redirect("/account/mail-instellingen?error=1");
                }
            }

            return Results.Redirect("/account/mail-instellingen?saved=1");
        }).RequireAuthorization();

        return app;
    }
}
