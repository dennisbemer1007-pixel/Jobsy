using System.Net;

namespace Jobsy.Tests.Errors;

/// <summary>
/// The factory wires an API client that throws on every call. If either page renders,
/// it proved it needs no API to do so (errors 01: "no data calls").
/// </summary>
public class ErrorLayoutIsolationTests
{
    [Fact]
    public async Task Status_404_renders_fully_while_every_api_call_throws()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/status/404");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("err-layout", html, StringComparison.Ordinal);
        Assert.Contains("err-footer", html, StringComparison.Ordinal);
        Assert.Contains("Deze pagina bestaat niet", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_page_renders_fully_while_every_api_call_throws()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync(ErrorPagesWebFactory.ThrowPath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("err-layout", html, StringComparison.Ordinal);
        Assert.Contains("err-footer", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_pages_do_not_load_the_blazor_runtime()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var html = await (await client.GetAsync("/status/404")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("_framework/blazor.web.js", html, StringComparison.Ordinal);
        Assert.Contains("css/features/errors.css", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Footer_shows_the_brand_legal_line_only()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/status/404")).Content.ReadAsStringAsync());

        Assert.Contains($"© {DateTime.UtcNow.Year} Lobsy", html, StringComparison.Ordinal);
    }
}
