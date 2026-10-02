using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests.Errors;

public class ErrorCultureTests
{
    [Theory]
    [InlineData("ar", "ar")]
    [InlineData("pl", "pl")]
    [InlineData("klingon", "nl")]
    [InlineData(null, "nl")]
    public void Cookie_wins_over_everything(string? cookie, string expected)
    {
        var http = new DefaultHttpContext();
        if (cookie is not null)
        {
            http.Request.Headers.Cookie = $"{CultureState.CookieName}={cookie}";
        }

        Assert.Equal(expected, ErrorCulture.Resolve(http));
    }

    [Theory]
    [InlineData("pl", "pl")]
    [InlineData("ro-RO,ro;q=0.9,en;q=0.8", "ro")]
    [InlineData("de-DE,de;q=0.9", "nl")]
    [InlineData("en;q=0.4,ar;q=0.9", "ar")]
    [InlineData("", "nl")]
    public void Accept_language_is_the_fallback(string header, string expected)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.AcceptLanguage = header;

        Assert.Equal(expected, ErrorCulture.Resolve(http));
    }

    [Fact]
    public void Cookie_beats_accept_language()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Cookie = $"{CultureState.CookieName}=pl";
        http.Request.Headers.AcceptLanguage = "ar";

        Assert.Equal("pl", ErrorCulture.Resolve(http));
    }

    [Fact]
    public void Arabic_is_right_to_left_and_dutch_is_not()
    {
        Assert.True(ErrorCulture.IsRightToLeft("ar"));
        Assert.False(ErrorCulture.IsRightToLeft("nl"));
    }

    [Fact]
    public void Status_copy_exists_in_all_five_languages()
    {
        string[] keys =
        [
            "Status.NotFound.Title",
            "Status.NotFound.Lead",
            "Status.Error.Title",
            "Status.Error.Lead",
            "Status.Common.CodeLabel",
            "Status.Nav.Login"
        ];

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            foreach (var key in keys)
            {
                var value = UiStrings.Get(key, lang);
                Assert.NotEqual(key, value);
                Assert.False(string.IsNullOrWhiteSpace(value));
            }
        }
    }
}

public class ErrorCultureRenderTests
{
    private static async Task<string> ReadStatusPageAsync(HttpClient client)
    {
        var response = await client.GetAsync("/status/404");
        // Razor encodes everything outside Basic Latin as numeric entities.
        return System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Arabic_cookie_renders_rtl_and_arabic_copy()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{CultureState.CookieName}=ar");

        var html = await ReadStatusPageAsync(client);

        Assert.Contains("dir=\"rtl\"", html, StringComparison.Ordinal);
        Assert.Contains("هذه الصفحة غير موجودة", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Polish_accept_language_renders_polish_copy()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "pl");

        var html = await ReadStatusPageAsync(client);

        Assert.Contains("Ta strona nie istnieje", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_language_falls_back_to_dutch()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.9");

        var html = await ReadStatusPageAsync(client);

        Assert.Contains("Deze pagina bestaat niet", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dir=\"rtl\"", html, StringComparison.Ordinal);
    }
}
