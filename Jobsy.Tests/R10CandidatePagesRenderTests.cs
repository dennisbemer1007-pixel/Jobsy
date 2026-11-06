using System.Net;
using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

[Collection("SequentialApi")]
public sealed class R10CandidatePagesRenderTests(R10CandidateHubWebFixture fixture) : IClassFixture<R10CandidateHubWebFixture>
{
    public static IEnumerable<object[]> CandidateHubRoutes =>
    [
        ["/candidate/settings?pane=mail", "candidate-settings-ssr"],
        ["/candidate/profile", "candidate-profile-ssr"],
        ["/profiel", "candidate-profile-ssr"],
        ["/candidate/external/add", "kb-ext-add"],
        ["/candidate/adviseur-werk", "uwv-adviseur-share"],
        ["/candidate/ondernemerscheck", "uwv-ondernemer-intro-page"],
        ["/candidate/ondernemerscheck?stap=question", "data-uwv-ondernemer-question"],
    ];

    [Theory]
    [MemberData(nameof(CandidateHubRoutes))]
    public async Task Authenticated_candidate_hub_routes_render_without_circuit_error(string path, string marker)
    {
        var baseUrl = fixture.ServerAddress;
        using var handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true };
        using var http = new HttpClient(handler);
        await http.GetAsync(
            $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(path)}");
        var response = await http.GetAsync($"{baseUrl}{path}");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(marker, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Even iets misgegaan", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LB-BFW", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Uwv_adviseur_deel_invalid_token_returns_404_without_server_error()
    {
        await using var web = new IllustratedPagesWebFactory();
        using var http = web.CreateClient(new() { AllowAutoRedirect = false });
        var response = await http.GetAsync("/deel/uwv-adviseur/not-a-valid-token");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var hasExpiredCopy = html.Contains("Link verlopen", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Link expired", StringComparison.OrdinalIgnoreCase);
        var hasStandard404 = html.Contains("data-testid=\"not-found-view\"", StringComparison.Ordinal)
            || html.Contains("Deze pagina bestaat niet", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasExpiredCopy || hasStandard404);
        Assert.DoesNotContain("Even iets misgegaan", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wwwroot_asset_catalog_covers_all_catalog_url_refs_in_host_pages()
    {
        var root = TestRepo.FindRoot();
        var missing = WwwrootAssetCatalogRefScanner.FindMissingManifestEntries(root);
        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }
}

public sealed class R10CandidateHubWebFixture : IAsyncDisposable
{
    private readonly IllustratedPagesWebFactory _web = new();

    public string ServerAddress { get; }

    public R10CandidateHubWebFixture()
    {
        ServerAddress = _web.ServerAddress;
    }

    public ValueTask DisposeAsync() => _web.DisposeAsync();
}

internal static class WwwrootAssetCatalogRefScanner
{
    private static readonly System.Text.RegularExpressions.Regex CatalogRef = new(
        @"WwwrootAssetCatalog\.Url(?:LeadingSlash)?\(\s*""([^""]+)""\s*\)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    internal static List<string> FindMissingManifestEntries(string repoRoot)
    {
        var www = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot");
        var missing = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(repoRoot, "Jobsy.Web"), "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            if (Path.GetExtension(file) is not (".razor" or ".cs"))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in CatalogRef.Matches(text))
            {
                var rel = match.Groups[1].Value;
                if (!File.Exists(Path.Combine(www, rel)))
                {
                    continue;
                }

                try
                {
                    _ = WwwrootAssetCatalog.Version(rel);
                }
                catch (InvalidOperationException)
                {
                    missing.Add($"{rel} (referenced in {Path.GetRelativePath(repoRoot, file)})");
                }
            }
        }

        return missing.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
}
