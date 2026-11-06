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
                return Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString("/candidate/settings?pane=mail"));
            }

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect(MailSettingsRedirect.Resolve(http, saved: false, error: false));
            }

            var form = await http.Request.ReadFormAsync();
            var keys = handler;
            var items = keys.Select(k => new
            {
                key = k,
                label = k,
                enabled = form.ContainsKey("pref_" + k)
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

            var jwt = accessTokens.TryCreate(http.User, http.Connection.RemoteIpAddress?.ToString());
            using var request = new HttpRequestMessage(HttpMethod.Put, "api/me/email-preferences")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { items, reminderEmailsEnabled }),
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
                return Results.Redirect(MailSettingsRedirect.Resolve(http, saved: false, error: true));
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
                    return Results.Redirect(MailSettingsRedirect.Resolve(http, saved: false, error: true));
                }
            }

            return Results.Redirect(MailSettingsRedirect.Resolve(http, saved: true, error: false));
        }).RequireAuthorization().WithOrder(-1000);

        return app;
    }
}

internal static class MailSettingsRedirect
{
    internal static string Resolve(HttpContext http, bool saved, bool error)
    {
        var candidateSettings = "/candidate/settings?pane=mail";
        var formReturn = http.Request.HasFormContentType
            ? http.Request.Form["settings_return"].ToString()
            : string.Empty;
        if (IsCandidateSettingsReturn(formReturn))
        {
            return AppendQuery(formReturn, saved, error);
        }

        var referer = http.Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer)
            && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
            && string.Equals(refererUri.Host, http.Request.Host.Host, StringComparison.OrdinalIgnoreCase)
            && refererUri.AbsolutePath.StartsWith("/candidate/settings", StringComparison.OrdinalIgnoreCase))
        {
            return AppendQuery(refererUri.PathAndQuery, saved, error);
        }

        if (saved || error)
        {
            return AppendQuery(candidateSettings, saved, error);
        }

        return "/account/mail-instellingen";
    }

    private static bool IsCandidateSettingsReturn(string? pathAndQuery)
        => !string.IsNullOrWhiteSpace(pathAndQuery)
           && pathAndQuery.StartsWith("/candidate/settings", StringComparison.OrdinalIgnoreCase)
           && !pathAndQuery.Contains("//", StringComparison.Ordinal);

    private static string AppendQuery(string pathAndQuery, bool saved, bool error)
    {
        var separator = pathAndQuery.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        if (saved)
        {
            return pathAndQuery + separator + "saved=1";
        }

        if (error)
        {
            return pathAndQuery + separator + "error=1";
        }

        return pathAndQuery;
    }
}
