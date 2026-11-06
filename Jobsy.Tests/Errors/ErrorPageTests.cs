using System.Net;
using System.Text.RegularExpressions;

namespace Jobsy.Tests.Errors;

public class ErrorPageTests
{
    private static readonly Regex SupportCodePattern = new("LB-[2-9A-HJKMNP-TV-Z]{4}", RegexOptions.Compiled);

    [Fact]
    public async Task Thrown_request_answers_500_with_a_support_code_and_no_internals()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync(ErrorPagesWebFactory.ThrowPath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Er ging iets mis", html, StringComparison.Ordinal);

        var code = SupportCodePattern.Match(html);
        Assert.True(code.Success, "No LB-XXXX support code on the 500 page.");

        Assert.DoesNotContain("Secret boom detail", html, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Referentie:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("TraceIdentifier", html, StringComparison.Ordinal);
        // The failing path may only appear as the "try again" href, never as visible text.
        Assert.DoesNotContain($">{ErrorPagesWebFactory.ThrowPath}<", html, StringComparison.Ordinal);
        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Support_code_is_logged_exactly_once_with_the_page_value()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var html = await (await client.GetAsync(ErrorPagesWebFactory.ThrowPath)).Content.ReadAsStringAsync();
        var code = SupportCodePattern.Match(html).Value;
        Assert.False(string.IsNullOrEmpty(code));

        var logged = factory.Logs.Messages
            .Where(m => m.Contains("Unhandled error", StringComparison.Ordinal)
                        && m.Contains(code, StringComparison.Ordinal))
            .ToList();

        Assert.Single(logged);
    }

    [Fact]
    public async Task Post_that_throws_also_renders_the_error_page()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.PostAsync(
            ErrorPagesWebFactory.ThrowPath,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["x"] = "1" }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Er ging iets mis", html, StringComparison.Ordinal);
        Assert.Matches(SupportCodePattern, html);
    }

    [Fact]
    public async Task Get_that_throws_offers_the_failing_page_as_the_retry_link()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var html = await (await client.GetAsync(ErrorPagesWebFactory.ThrowPath)).Content.ReadAsStringAsync();

        Assert.Contains($"href=\"{ErrorPagesWebFactory.ThrowPath}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_page_offers_a_mail_link_with_the_code_in_the_subject()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var html = await (await client.GetAsync(ErrorPagesWebFactory.ThrowPath)).Content.ReadAsStringAsync();
        var code = SupportCodePattern.Match(html).Value;

        Assert.Contains($"mailto:support@lobsy.nl?subject=Foutcode%20{code}", html, StringComparison.Ordinal);
    }
}
