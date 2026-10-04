using System.Net.Http.Json;
using System.Text.Json;

namespace Jobsy.Web.Auth;

public static class ReferenceConfirmationEndpoints
{
    public static IEndpointRouteBuilder MapReferenceConfirmationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/referentie/{token}", async (
            string token,
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration) =>
        {
            if (!IsToken(token))
            {
                return Results.Redirect("/referentie/ongeldig?fout=ongeldig");
            }

            var form = http.Request.HasFormContentType
                ? await http.Request.ReadFormAsync()
                : null;
            var action = form?["action"].ToString() ?? "submit";
            var apiBase = (configuration["ApiBaseUrl"] ?? configuration["JobsyApi:BaseUrl"] ?? "http://localhost:5200").TrimEnd('/');
            var client = httpClientFactory.CreateClient();
            HttpResponseMessage response;
            if (string.Equals(action, "decline", StringComparison.OrdinalIgnoreCase))
            {
                response = await client.PostAsync($"{apiBase}/api/public/reference-confirmations/{token}/decline", null);
                return Redirect(token, response.IsSuccessStatusCode ? "klaar=nee" : await Fault(response));
            }

            if (string.Equals(action, "misuse", StringComparison.OrdinalIgnoreCase))
            {
                response = await client.PostAsJsonAsync(
                    $"{apiBase}/api/public/reference-confirmations/{token}/misuse",
                    new { message = form?["message"].ToString() });
                return Redirect(token, response.IsSuccessStatusCode ? "klaar=melding" : await Fault(response));
            }

            var workedRaw = form?["worked"].ToString();
            bool? worked = workedRaw switch
            {
                "ja" => true,
                "nee" => false,
                _ => null
            };
            response = await client.PostAsJsonAsync(
                $"{apiBase}/api/public/reference-confirmations/{token}/submit",
                new
                {
                    workedHere = worked,
                    period = form?["period"].ToString(),
                    didWell = form?["didWell"].ToString(),
                    workAgain = form?["workAgain"].ToString(),
                    extra = form?["extra"].ToString()
                });
            return Redirect(token, response.IsSuccessStatusCode ? "klaar=bevestigd" : await Fault(response));
        })
        .AllowAnonymous()
        .DisableAntiforgery()
        .RequireRateLimiting("reference-form");

        return app;
    }

    private static IResult Redirect(string token, string query)
        => Results.Redirect($"/referentie/{Uri.EscapeDataString(token)}?{query}");

    private static async Task<string> Fault(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var error = doc.RootElement.TryGetProperty("error", out var value) ? value.GetString() : null;
            return error switch
            {
                "expired" => "fout=verlopen",
                "used" => "fout=gebruikt",
                "answers" => "fout=invullen",
                _ => "fout=ongeldig"
            };
        }
        catch (JsonException)
        {
            return "fout=ongeldig";
        }
    }

    private static bool IsToken(string token)
        => token.Length == 64 && token.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
