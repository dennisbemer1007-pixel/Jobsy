using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Auth;

public static class MailUnsubscribeEndpoints
{
    public static IEndpointRouteBuilder MapMailUnsubscribeEndpoints(this IEndpointRouteBuilder app)
    {
        // RFC 8058 one-click + browser form POST. Antiforgery disabled; rate-limited.
        app.MapPost("/mail/afmelden", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration) =>
        {
            string? token = null;
            string? action = null;

            if (http.Request.HasFormContentType)
            {
                var form = await http.Request.ReadFormAsync();
                token = form["t"].ToString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    token = form["token"].ToString();
                }

                action = form["action"].ToString();
                // RFC 8058 body may also arrive as form: List-Unsubscribe=One-Click
            }
            else
            {
                using var reader = new StreamReader(http.Request.Body, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                if (body.Contains("List-Unsubscribe=One-Click", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(body.Trim(), "List-Unsubscribe=One-Click", StringComparison.OrdinalIgnoreCase))
                {
                    token = http.Request.Query["t"].FirstOrDefault();
                }
                else
                {
                    // Try JSON
                    try
                    {
                        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                        if (doc.RootElement.TryGetProperty("token", out var t))
                        {
                            token = t.GetString();
                        }

                        if (doc.RootElement.TryGetProperty("action", out var a))
                        {
                            action = a.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                        token = http.Request.Query["t"].FirstOrDefault();
                    }
                }
            }

            token ??= http.Request.Query["t"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(token))
            {
                return HtmlError(http);
            }

            var apiBase = (configuration["ApiBaseUrl"] ?? configuration["JobsyApi:BaseUrl"] ?? "http://localhost:5200").TrimEnd('/');
            var client = httpClientFactory.CreateClient();
            var payload = new Dictionary<string, string?> { ["token"] = token };
            if (!string.IsNullOrWhiteSpace(action))
            {
                payload["action"] = action;
            }

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync($"{apiBase}/api/email-preferences/unsubscribe", content);

            var acceptsHtml = http.Request.Headers.Accept.Any(a =>
                a is not null && a.Contains("text/html", StringComparison.OrdinalIgnoreCase));
            var isBrowserForm = http.Request.HasFormContentType
                && !string.Equals(http.Request.Headers["List-Unsubscribe"], "One-Click", StringComparison.OrdinalIgnoreCase)
                && acceptsHtml;

            if (!response.IsSuccessStatusCode)
            {
                return HtmlError(http, token);
            }

            if (isBrowserForm)
            {
                var done = string.Equals(action, "opt-in", StringComparison.OrdinalIgnoreCase) ? "optin" : "1";
                return Results.Redirect($"/mail/afmelden?done={done}&t=" + Uri.EscapeDataString(token));
            }

            // RFC 8058 / mailbox providers: 200 plain HTML
            return Results.Content(
                "<!DOCTYPE html><body><p>Je krijgt geen herinneringen meer per e-mail. Je kunt dit weer aanzetten in je instellingen.</p></body>",
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status200OK);
        })
        .AllowAnonymous()
        .DisableAntiforgery()
        .RequireRateLimiting("mail-unsubscribe")
        // The Blazor page lives on the same path. This POST must win for the form and for RFC 8058.
        .WithOrder(-1000);

        return app;
    }

    private static IResult HtmlError(HttpContext http, string? token = null)
    {
        var oneClick = string.Equals(http.Request.Headers["List-Unsubscribe"], "One-Click", StringComparison.OrdinalIgnoreCase);
        var acceptsHtml = http.Request.Headers.Accept.Any(a =>
            a is not null && a.Contains("text/html", StringComparison.OrdinalIgnoreCase));
        if (!oneClick && (http.Request.HasFormContentType || acceptsHtml || http.Request.Headers.Accept.Count == 0))
        {
            var target = "/mail/afmelden?error=1";
            if (!string.IsNullOrWhiteSpace(token))
            {
                target += "&t=" + Uri.EscapeDataString(token);
            }

            return Results.Redirect(target);
        }

        return Results.Text("De link is ongeldig of verlopen.", "text/plain", statusCode: StatusCodes.Status400BadRequest);
    }
}
