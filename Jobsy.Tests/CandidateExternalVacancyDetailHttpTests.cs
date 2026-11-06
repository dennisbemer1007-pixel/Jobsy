using System.Net;

namespace Jobsy.Tests;

[Collection("SequentialApi")]
public sealed class CandidateExternalVacancyDetailHttpTests
{
    /// <summary>Acc-like: purged external id → API 404 → SSR NotFoundView with HTTP 404.</summary>
    public static readonly Guid PurgedExternalId = Guid.Parse("73349dcd-0000-4000-8000-000000000099");

    [Fact]
    public void GetExternalVacancyAsync_treats_api_404_as_null_not_exception()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Services/ApiClient/JobsyApiClient.CandidateExternalVacancies.cs"));
        Assert.Contains("StatusCode == System.Net.HttpStatusCode.NotFound", source, StringComparison.Ordinal);
        Assert.Contains("return null", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "GetApiJsonAsync<ExternalVacancyDetailDto>($\"api/candidate/external-vacancies/{id:D}\"",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Purged_external_detail_page_returns_404_not_error_inline()
    {
        await using var web = new IllustratedPagesWebFactory();
        using var handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(web.ServerAddress) };
        await http.GetAsync(
            $"/__test/sign-in?role=candidate&return={Uri.EscapeDataString($"/candidate/external/{PurgedExternalId:D}")}");
        var response = await http.GetAsync($"/candidate/external/{PurgedExternalId:D}");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("data-testid=\"not-found-view\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Dit onderdeel laadt even niet", html, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
