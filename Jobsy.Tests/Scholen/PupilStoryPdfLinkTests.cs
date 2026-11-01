using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests.Scholen;

public sealed class PupilStoryPdfLinkTests
{
    [Theory]
    [InlineData(null, null, "/leerling/pdf")]
    [InlineData("Noor", "nl", "/leerling/pdf?naam=Noor&lang=nl")]
    [InlineData(null, "en", "/leerling/pdf?lang=en")]
    public void Href_builds_valid_query(string? name, string? lang, string expected)
    {
        Assert.Equal(expected, PupilStoryPdfLink.Href(name, lang));
    }

    [Theory]
    [InlineData("?lang=nl", "nl")]
    [InlineData("lang=nl?", "nl")]
    [InlineData("nl?", "nl")]
    [InlineData("  en  ", "en")]
    public void NormalizeLanguageCode_strips_malformed_prefixes(string raw, string expected)
    {
        Assert.Equal(expected, PupilStoryPdfLink.NormalizeLanguageCode(raw));
    }

    [Theory]
    [InlineData("/leerling/pdf?lang=nl?", "/leerling/pdf?lang=nl")]
    [InlineData("/leerling/pdf?naam=Noor&lang=nl?", "/leerling/pdf?naam=Noor&lang=nl")]
    public void SanitizeHref_strips_trailing_question_marks(string raw, string expected)
    {
        Assert.Equal(expected, PupilStoryPdfLink.SanitizeHref(raw));
    }

    [Fact]
    public void Href_never_contains_double_question_mark()
    {
        foreach (var lang in new[] { "nl", "?lang=nl", "lang=nl?", "nl?", "nl? " })
        {
            var href = PupilStoryPdfLink.Href("Alex", lang);
            Assert.DoesNotContain("??", href, StringComparison.Ordinal);
            Assert.DoesNotContain("?lang=nl?", href, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(@"/leerling/pdf\?$", href);
            Assert.DoesNotMatch(@"/leerling/pdf\?lang=nl\?$", href);
        }
    }

    [Fact]
    public void HrefFromQuery_rebuilds_canonical_link()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?lang=nl?&naam=Noor");
        Assert.Equal("/leerling/pdf?naam=Noor&lang=nl", PupilStoryPdfLink.HrefFromQuery(http.Request.Query));
        http.Request.Path = PupilStoryPdfLink.Path;
        Assert.True(PupilStoryPdfLink.NeedsCanonicalRedirect(http.Request));
    }

    [Fact]
    public void NeedsCanonicalRedirect_false_for_clean_url()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = PupilStoryPdfLink.Path;
        http.Request.QueryString = new QueryString("?lang=nl");
        Assert.False(PupilStoryPdfLink.NeedsCanonicalRedirect(http.Request));
    }
}
