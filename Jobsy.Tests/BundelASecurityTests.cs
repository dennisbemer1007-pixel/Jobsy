using Microsoft.AspNetCore.Hosting;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Security;
using Jobsy.Tests.Uat;
using Jobsy.Web.Security;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class BundelASecurityTests
{
    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<svg onload=alert(1)>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<p onclick=alert(1)>x</p>")]
    [InlineData("<script>alert(1)</script>")]
    public void HtmlSanitize_xss_corpus_strips_dangerous_markup(string payload)
    {
        var output = HtmlSanitize.ToVacancyDescriptionHtml("<p>" + payload + "</p>");
        Assert.DoesNotContain("onerror", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void G78_why_fit_is_html_encoded_in_ssr()
    {
        const string xss = "<img src=x onerror=alert(1)>";
        var html = PupilLeerlingSsrParts.Groep78JobWhyPanel("kok", "Kok", xss);
        Assert.Contains("&lt;img", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bff_same_origin_rejects_cross_origin_header()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicWebBaseUrl"] = "https://lobsy.nl" })
            .Build();
        var http = new DefaultHttpContext();
        http.Request.Headers.Origin = "https://evil.example";
        http.Request.Headers["Sec-Fetch-Site"] = "cross-site";

        Assert.False(BffMutatingSecurityFilter.IsSameOrigin(http, config));
    }

    [Fact]
    public void Every_routable_page_has_authorize_or_allow_anonymous()
    {
        var index = RazorRouteIndex.Load();
        var missing = index.Pages
            .Where(p => !p.AllowAnonymous && !p.Authorize)
            .Select(p => p.FilePath)
            .ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Upload_magic_rejects_exe_with_pdf_content_type()
    {
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        Assert.False(UploadFileMagicRules.TryValidateContent("application/pdf", exe, out _));
    }

    [Fact]
    public void Class_pause_requires_distinct_partitions_before_threshold()
    {
        var protection = new PupilLoginProtection();
        var classId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        for (var i = 0; i < PupilLoginProtection.ClassHourFailLimit; i++)
        {
            protection.RecordClassHourFailure(classId, "same-partition");
        }

        Assert.Equal(1, protection.GetClassHourDistinctPartitions(classId));
        Assert.Equal(PupilLoginProtection.ClassHourFailLimit, protection.GetClassHourFailureCount(classId));
    }
}

public sealed class WebDevRouteTests : IClassFixture<WebDevRouteFactory>
{
    private readonly WebDevRouteFactory _factory;

    public WebDevRouteTests(WebDevRouteFactory factory) => _factory = factory;

    [Fact]
    public async Task Dev_route_returns_404_in_Production()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/dev/na-de-match/01");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class WebDevRouteFactory : TestSupport.WebWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        JobsyTestAuth.ApplyProductionJwtSettings(builder);
        JobsyTestAuth.ApplyProductionOriginSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("JobsyAuth:AllowEphemeralDataProtection", "true");
    }
}

public sealed class BffAntiforgeryRejectionTests : IClassFixture<BffSecurityWebFactory>
{
    private readonly BffSecurityWebFactory _factory;

    public BffAntiforgeryRejectionTests(BffSecurityWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Bff_post_without_antiforgery_token_is_rejected()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", BffSecurityWebFactory.Origin);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
        using var content = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
        using var response = await client.PostAsync("/api/banenkaart-client/apply-filters", content);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class BffSecurityWebFactory : TestSupport.WebWebApplicationFactory
{
    public const string Origin = "https://localhost";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("PublicWebBaseUrl", Origin);
        builder.UseSetting("Seed:Enabled", "false");
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.BaseAddress = new Uri(Origin + "/");
    }
}
