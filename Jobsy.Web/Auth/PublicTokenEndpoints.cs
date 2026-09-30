using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Auth;

public static class PublicTokenEndpoints
{
    public static IEndpointRouteBuilder MapPublicTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/toestemming/confirm", async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration) =>
        {
            var form = await http.Request.ReadFormAsync();
            var token = form["t"].ToString();
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/toestemming?t=" + Uri.EscapeDataString(token));
            }

            var apiBase = (configuration["JobsyApi:BaseUrl"] ?? "http://localhost:5200").TrimEnd('/');
            var client = httpClientFactory.CreateClient();
            using var content = new StringContent(
                JsonSerializer.Serialize(new { token }),
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync($"{apiBase}/api/parental-consent/confirm", content);
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect("/toestemming?t=" + Uri.EscapeDataString(token));
            }

            return Results.Redirect("/toestemming?done=1");
        }).AllowAnonymous();

        app.MapPost("/account/wachtwoord-instellen", async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration) =>
        {
            var form = await http.Request.ReadFormAsync();
            var token = form["t"].ToString();
            var password = form["password"].ToString();
            var password2 = form["password2"].ToString();

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect($"/account/wachtwoord-instellen?t={Uri.EscapeDataString(token)}&error=invalid");
            }

            if (!string.Equals(password, password2, StringComparison.Ordinal))
            {
                return Results.Redirect($"/account/wachtwoord-instellen?t={Uri.EscapeDataString(token)}&error=mismatch");
            }

            var apiBase = (configuration["JobsyApi:BaseUrl"] ?? "http://localhost:5200").TrimEnd('/');
            var client = httpClientFactory.CreateClient();
            using var content = new StringContent(
                JsonSerializer.Serialize(new { token, password }),
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync($"{apiBase}/api/account/setup-password", content);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync();
                var error = body.Contains("Wachtwoord", StringComparison.OrdinalIgnoreCase) ? "rules" : "invalid";
                return Results.Redirect($"/account/wachtwoord-instellen?t={Uri.EscapeDataString(token)}&error={error}");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect($"/account/wachtwoord-instellen?t={Uri.EscapeDataString(token)}&error=invalid");
            }

            return Results.Redirect("/login?setup=done");
        }).AllowAnonymous();

        app.MapPost("/koppeling/sleutel", async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration) =>
        {
            var form = await http.Request.ReadFormAsync();
            var token = form["t"].ToString();
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect($"/koppeling/sleutel?t={Uri.EscapeDataString(token)}");
            }

            var apiBase = (configuration["JobsyApi:BaseUrl"] ?? "http://localhost:5200").TrimEnd('/');
            var client = httpClientFactory.CreateClient();
            using var content = new StringContent(
                JsonSerializer.Serialize(new { token }),
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync($"{apiBase}/api/company-api-keys/reveal", content);
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect("/koppeling/sleutel");
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            http.Items["ApiKeyReveal.Plaintext"] = root.GetProperty("plaintextKey").GetString();
            http.Items["ApiKeyReveal.CompanyName"] = root.TryGetProperty("companyName", out var cn) ? cn.GetString() : "Lobsy";
            http.Items["ApiKeyReveal.Endpoint"] = root.TryGetProperty("endpoint", out var ep) ? ep.GetString() : null;
            http.Items["ApiKeyReveal.Swagger"] = root.TryGetProperty("swaggerUrl", out var sw) ? sw.GetString() : null;

            // Rewrite to the page so Blazor SSR can read Items — use a short-lived cookie flash instead.
            var plaintext = http.Items["ApiKeyReveal.Plaintext"] as string ?? "";
            var company = http.Items["ApiKeyReveal.CompanyName"] as string ?? "Lobsy";
            var endpoint = http.Items["ApiKeyReveal.Endpoint"] as string ?? "";
            var swagger = http.Items["ApiKeyReveal.Swagger"] as string ?? "";
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { plaintext, company, endpoint, swagger })));
            http.Response.Cookies.Append(
                "lobsy_api_reveal",
                payload,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = http.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromMinutes(2),
                    IsEssential = true
                });
            return Results.Redirect("/koppeling/sleutel?shown=1");
        }).AllowAnonymous();

        return app;
    }
}
