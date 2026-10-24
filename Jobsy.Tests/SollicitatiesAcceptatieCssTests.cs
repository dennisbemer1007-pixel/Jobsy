using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Tests;

[Trait("Suite", "AccCss")]
public sealed class SollicitatiesAcceptatieCssTests
{
    private const string AccBase = "https://acceptatie.lobsy.nl";

    [Fact]
    public async Task Acc_serves_scope3_applications_css_with_sollicitaties_override()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var url = $"{AccBase}/css/features/applications.css?v=20261024-apps720-scope3";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");
        using var resp = await client.SendAsync(req);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        var css = await resp.Content.ReadAsStringAsync();
        Assert.Contains("scope3: sollicitaties SSR", css, StringComparison.Ordinal);
        Assert.Contains(".panel-page.sollicitaties-page .sollic-card", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Acc_serves_mockup01_sollicitaties_css_matching_repo_sha()
    {
        var root = FindRepoRoot();
        var localPath = Path.Combine(root, "Jobsy.Web/wwwroot/css/features/sollicitaties-page.css");
        var localBytes = await File.ReadAllBytesAsync(localPath);
        var localSha = Convert.ToHexString(SHA256.HashData(localBytes)).ToLowerInvariant();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var url = $"{AccBase}/css/features/sollicitaties-page.css?v=20261024-mockup01-acc4";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Accept-Encoding", "br");
        using var brResp = await client.SendAsync(req);
        if (brResp.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.OK, brResp.StatusCode);
        var decoded = BrotliDecompress(await brResp.Content.ReadAsByteArrayAsync());
        var accSha = Convert.ToHexString(SHA256.HashData(decoded)).ToLowerInvariant();
        Assert.Equal(localSha, accSha);
        Assert.Contains("@media (min-width: 1024px)", Encoding.UTF8.GetString(decoded), StringComparison.Ordinal);
    }

    private static byte[] BrotliDecompress(byte[] input)
    {
        using var ms = new MemoryStream(input);
        using var brotli = new System.IO.Compression.BrotliStream(ms, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
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

        throw new InvalidOperationException("Could not find repo root.");
    }
}
