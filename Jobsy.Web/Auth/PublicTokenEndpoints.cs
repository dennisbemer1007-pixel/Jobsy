using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobsy.Web.Localization;
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
            if (string.IsNullOrWhiteSpace(token))
            {
                token = form["token"].ToString();
            }

            var password = form["password"].ToString();
            var password2 = form["password2"].ToString();
            var isReset = string.Equals(form["doel"].ToString(), "reset", StringComparison.OrdinalIgnoreCase);

            string RedirectError(string error)
            {
                var q = isReset
                    ? $"token={Uri.EscapeDataString(token)}&doel=reset&error={error}"
                    : $"t={Uri.EscapeDataString(token)}&error={error}";
                return $"/account/wachtwoord-instellen?{q}";
            }

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect(RedirectError("invalid"));
            }

            if (!string.Equals(password, password2, StringComparison.Ordinal))
            {
                return Results.Redirect(RedirectError("mismatch"));
            }

            var apiBase = (configuration["JobsyApi:BaseUrl"] ?? configuration["ApiBaseUrl"] ?? "http://localhost:5200").TrimEnd('/');
            var client = isReset
                ? http.RequestServices.GetRequiredService<AuthApiClient>().CreateClient()
                : httpClientFactory.CreateClient();
            if (!isReset)
            {
                client.BaseAddress ??= new Uri(apiBase.EndsWith('/') ? apiBase : apiBase + "/");
            }

            var path = isReset ? "api/auth/password-reset/complete" : "api/account/setup-password";
            using var content = new StringContent(
                JsonSerializer.Serialize(new { token, password }),
                Encoding.UTF8,
                "application/json");
            var response = isReset
                ? await client.PostAsync(path, content)
                : await client.PostAsync($"{apiBase}/{path}", content);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync();
                var error = body.Contains("Wachtwoord", StringComparison.OrdinalIgnoreCase) ? "rules" : "invalid";
                return Results.Redirect(RedirectError(error));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect(RedirectError("invalid"));
            }

            return Results.Redirect("/login?setup=done");
        }).AllowAnonymous();

        app.MapPost("/account/wachtwoord-vergeten", async (
            HttpContext http,
            IAntiforgery antiforgery) =>
        {
            var form = await http.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim();
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/wachtwoord-vergeten");
            }

            var culture = http.Request.Cookies.TryGetValue(CultureState.CookieName, out var lang)
                ? lang
                : "nl";

            var authApi = http.RequestServices.GetRequiredService<AuthApiClient>();
            try
            {
                using var _ = await authApi.PostJsonAsync(
                    "api/auth/password-reset/request",
                    new { email, culture });
            }
            catch
            {
                // Still show the uniform sent screen.
            }

            return Results.Redirect("/wachtwoord-vergeten?sent=1");
        }).AllowAnonymous().RequireRateLimiting("auth");

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
