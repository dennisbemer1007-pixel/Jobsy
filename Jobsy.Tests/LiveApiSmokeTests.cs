using System.Net;
using System.Net.Http.Json;

namespace Jobsy.Tests;

/// <summary>
/// Live smoke checks against a running local API (http://localhost:5200).
/// Skipped automatically when the API is not reachable.
/// Uses the DevelopmentAuth secret from appsettings.Development.json.
/// </summary>
public class LiveApiSmokeTests
{
    private const string BaseUrl = "http://localhost:5200/";
    private const string DevSecret = "local-dev-jobsy-auth-secret";

    [Fact]
    public async Task Public_vacancies_endpoint_returns_ok()
    {
        using var client = CreateClient();
        if (!await IsApiUpAsync(client))
        {
            return; // soft-skip when API is down
        }

        var response = await client.GetAsync("api/vacancies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<VacancySmokeDto>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body);
        Assert.All(body!, v => Assert.False(string.IsNullOrWhiteSpace(v.Title)));
    }

    [Fact]
    public async Task Admin_metrics_summary_returns_expected_keys()
    {
        using var client = CreateClient();
        if (!await IsApiUpAsync(client))
        {
            return;
        }

        AddAdminAuth(client);

        var response = await client.GetAsync("api/metrics/summary?period=day");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metrics = await response.Content.ReadFromJsonAsync<List<MetricSmokeDto>>();
        Assert.NotNull(metrics);
        Assert.Contains(metrics!, m => m.Key == "active_vacancies");
        Assert.Contains(metrics!, m => m.Key == "clicks");
        Assert.Contains(metrics!, m => m.Key == "tokens_purchased");
    }

    [Fact]
    public async Task Integration_health_requires_admin()
    {
        using var client = CreateClient();
        if (!await IsApiUpAsync(client))
        {
            return;
        }

        var anon = await client.GetAsync("api/integrations/health");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        AddAdminAuth(client);
        var admin = await client.GetAsync("api/integrations/health");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task Kvk_establishments_are_reachable()
    {
        using var client = CreateClient();
        if (!await IsApiUpAsync(client))
        {
            return;
        }

        var response = await client.GetAsync("api/kvk/12345678/establishments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void AddAdminAuth(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-Jobsy-Email");
        client.DefaultRequestHeaders.Remove("X-Jobsy-Dev-Secret");
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", "admin@jobsy.local");
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", DevSecret);
    }

    private static HttpClient CreateClient() => new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(3) };

    private static async Task<bool> IsApiUpAsync(HttpClient client)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await client.GetAsync("api/vacancies", cts.Token);
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized;
        }
        catch
        {
            return false;
        }
    }

    private sealed record VacancySmokeDto(string Title);
    private sealed record MetricSmokeDto(string Key, string Label, string Period, decimal Value);
}
