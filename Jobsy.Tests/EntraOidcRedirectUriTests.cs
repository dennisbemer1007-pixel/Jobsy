using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Tests;

public class EntraOidcRedirectUriTests
{
    [Fact]
    public void Build_uses_PublicWebBaseUrl_when_configured()
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("lobsy-acc-web.onrender.com");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "https://acceptatie.lobsy.nl"
            })
            .Build();

        var uri = EntraOidcRedirectUri.Build(http, config, "/signin-entra");
        Assert.Equal("https://acceptatie.lobsy.nl/signin-entra", uri);
    }

    [Fact]
    public void ResolveScheme_honours_X_Forwarded_Proto()
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "http";
        http.Request.Headers["X-Forwarded-Proto"] = "https, https";

        Assert.Equal(Uri.UriSchemeHttps, EntraOidcRedirectUri.ResolveScheme(http));
    }

    [Fact]
    public void Entra_oidc_uses_query_response_mode_and_root_cookie_path()
    {
        var auth = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Auth", "AuthServiceCollectionExtensions.cs"));
        Assert.Contains("OpenIdConnectResponseMode.Query", auth, StringComparison.Ordinal);
        Assert.Contains("CorrelationCookie.Path = \"/\"", auth, StringComparison.Ordinal);
        Assert.Contains("CorrelationCookie.SameSite = SameSiteMode.None", auth, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
