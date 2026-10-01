using System.Net;

namespace Jobsy.Tests.Errors;

/// <summary>
/// When even the layout cannot render, the visitor still gets readable HTML and the
/// right status code — never a blank body.
/// </summary>
public class ErrorLayoutFallbackTests
{
    [Fact]
    public async Task Broken_layout_on_an_unknown_page_still_answers_404_with_minimal_html()
    {
        await using var factory = new ErrorPagesWebFactory { BreakLayout = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/bestaat-niet");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Deze pagina bestaat niet.", html, StringComparison.Ordinal);
        Assert.Contains("This page does not exist.", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stylesheet", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Chrome is broken", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Broken_layout_on_a_thrown_request_still_answers_500_with_minimal_html()
    {
        await using var factory = new ErrorPagesWebFactory { BreakLayout = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync(ErrorPagesWebFactory.ThrowPath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Er ging iets mis.", html, StringComparison.Ordinal);
        Assert.Contains("Something went wrong.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret boom detail", html, StringComparison.Ordinal);
    }
}
