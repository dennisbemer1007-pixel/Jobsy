using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class HtmlSanitizeTests
{
    [Fact]
    public void ToSafeMarkup_allows_basic_about_tags()
    {
        var html = "<section><h2>Hallo</h2><p>Tekst met <strong>vet</strong> en <a href=\"mailto:privacy@lobsy.nl\">mail</a>.</p><ul><li>een</li></ul></section>";
        var safe = HtmlSanitize.ToSafeMarkup(html);

        Assert.Contains("<h2>Hallo</h2>", safe);
        Assert.Contains("<strong>vet</strong>", safe);
        Assert.Contains("mailto:privacy@lobsy.nl", safe);
        Assert.Contains("<ul>", safe);
    }

    [Fact]
    public void ToSafeMarkup_strips_script_and_handlers()
    {
        var html = "<p onclick=\"alert(1)\">Ok</p><script>alert(1)</script><a href=\"javascript:alert(1)\">x</a>";
        var safe = HtmlSanitize.ToSafeMarkup(html);

        Assert.DoesNotContain("script", safe, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", safe, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", safe, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Ok</p>", safe);
    }
}
