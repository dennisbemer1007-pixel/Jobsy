using System.Net;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Errors;

/// <summary>
/// errors 04 §04.3: the reconnect toast is part of the static App shell, so it must read the
/// catalog with the request language instead of hard-coded Dutch.
/// </summary>
public class ReconnectToastTests
{
    private static readonly string[] Keys =
    [
        "Status.Reconnect.Trying",
        "Status.Reconnect.Failed",
        "Status.Reconnect.Rejected",
        "Status.Reconnect.Reload"
    ];

    [Fact]
    public async Task App_shell_renders_the_toast_in_dutch_by_default()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();

        // Razor encodes non-ASCII (the ellipsis, Arabic) as numeric entities; compare decoded.
        var html = WebUtility.HtmlDecode(
            await (await client.GetAsync("/status/404")).Content.ReadAsStringAsync());

        Assert.Contains("id=\"components-reconnect-modal\"", html, StringComparison.Ordinal);
        foreach (var key in Keys)
        {
            Assert.Contains(UiStrings.Get(key, "nl"), html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task App_shell_renders_the_toast_in_arabic_when_the_culture_cookie_says_so()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{CultureState.CookieName}=ar");

        // Razor encodes non-ASCII (the ellipsis, Arabic) as numeric entities; compare decoded.
        var html = WebUtility.HtmlDecode(
            await (await client.GetAsync("/status/404")).Content.ReadAsStringAsync());

        Assert.Contains("dir=\"rtl\"", html, StringComparison.Ordinal);
        foreach (var key in Keys)
        {
            Assert.Contains(UiStrings.Get(key, "ar"), html, StringComparison.Ordinal);
            Assert.DoesNotContain(UiStrings.Get(key, "nl"), html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void App_razor_has_no_hard_coded_reconnect_text()
    {
        var app = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Components", "App.razor"));

        Assert.DoesNotContain("Verbinding herstellen", app, StringComparison.Ordinal);
        Assert.DoesNotContain("Verbinding verbroken", app, StringComparison.Ordinal);
        Assert.DoesNotContain("Sessie verlopen", app, StringComparison.Ordinal);
        foreach (var key in Keys)
        {
            Assert.Contains(key, app, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_toast_keys_exist_in_all_five_languages()
    {
        foreach (var key in Keys)
        {
            foreach (var language in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var text = UiStrings.Get(key, language);
                Assert.False(string.IsNullOrWhiteSpace(text), $"{key} missing for {language}");
                Assert.NotEqual(key, text);
            }
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Jobsy.sln not found");
    }
}
