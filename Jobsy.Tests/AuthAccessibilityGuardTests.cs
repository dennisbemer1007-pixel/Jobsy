using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public sealed class AuthAccessibilityGuardTests
{
    [Theory]
    [InlineData("/login")]
    [InlineData("/login?lang=ar")]
    [InlineData("/wachtwoord-vergeten")]
    [InlineData("/wachtwoord-vergeten?lang=ar")]
    public async Task Public_auth_pages_have_landmarks_labels_and_dir(string path)
    {
        await using var factory = new AuthA11yWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(1, Regex.Matches(html, @"<main\b", RegexOptions.IgnoreCase).Count);
        Assert.Equal(1, Regex.Matches(html, @"<h1\b", RegexOptions.IgnoreCase).Count);

        var isAr = path.Contains("lang=ar", StringComparison.Ordinal);
        if (isAr)
        {
            Assert.Matches(@"dir\s*=\s*""rtl""", html);
            Assert.Contains("lang=\"ar\"", html, StringComparison.OrdinalIgnoreCase);
        }

        foreach (Match m in Regex.Matches(html, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var tag = m.Value;
            if (Regex.IsMatch(tag, @"type\s*=\s*""hidden""", RegexOptions.IgnoreCase)
                || Regex.IsMatch(tag, @"type\s*=\s*""checkbox""", RegexOptions.IgnoreCase))
            {
                continue;
            }

            var idMatch = Regex.Match(tag, @"\bid\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
            Assert.True(idMatch.Success, "Visible input missing id: " + tag);
            var id = idMatch.Groups[1].Value;
            Assert.Contains($"for=\"{id}\"", html, StringComparison.Ordinal);

            if (Regex.IsMatch(tag, @"type\s*=\s*""(email|password|text)""", RegexOptions.IgnoreCase)
                || tag.Contains("autocomplete=\"username\"", StringComparison.OrdinalIgnoreCase)
                || tag.Contains("autocomplete=\"current-password\"", StringComparison.OrdinalIgnoreCase)
                || tag.Contains("autocomplete=\"new-password\"", StringComparison.OrdinalIgnoreCase)
                || tag.Contains("autocomplete=\"one-time-code\"", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains("dir=\"ltr\"", tag, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (html.Contains("error=invalid", StringComparison.Ordinal)
            || html.Contains("au-alert--danger", StringComparison.Ordinal))
        {
            Assert.Contains("role=\"alert\"", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Login_invalid_error_exposes_alert_and_aria_invalid()
    {
        await using var factory = new AuthA11yWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await client.GetStringAsync("/login?error=invalid");
        Assert.Contains("role=\"alert\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-invalid=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=", html, StringComparison.Ordinal);
        Assert.Contains("autofocus", html, StringComparison.OrdinalIgnoreCase);
    }
}

file sealed class AuthA11yWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:AllowDevelopmentAuth"] = "true",
                ["PublicWebBaseUrl"] = "http://localhost"
            });
        });
        builder.ConfigureTestServices(_ => { });
    }
}
