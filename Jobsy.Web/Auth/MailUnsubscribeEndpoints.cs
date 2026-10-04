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
                return Results.Content(
                    "<!DOCTYPE html><body><p>De link is ongeldig of verlopen.</p></body>",
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status400BadRequest);
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
                if (isBrowserForm)
                {
                    return Results.Redirect("/mail/afmelden?error=1&t=" + Uri.EscapeDataString(token));
                }

                return Results.Content(
                    "<!DOCTYPE html><body><p>De link is ongeldig of verlopen.</p></body>",
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status400BadRequest);
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
}
