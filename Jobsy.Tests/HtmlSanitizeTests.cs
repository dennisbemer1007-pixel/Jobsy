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
    public void ToVacancyDescriptionHtml_allows_headings_and_brand_color()
    {
        var html = "<h2>Kop</h2><p style=\"text-align:center\">Midden</p><p><span style=\"color:#0f2d5c\">Blauw</span></p>";
        var safe = HtmlSanitize.ToVacancyDescriptionHtml(html);

        Assert.Contains("<h2>Kop</h2>", safe);
        Assert.Contains("text-align:center", safe);
        Assert.Contains("color:#0f2d5c", safe);
    }

    [Fact]
    public void ToVacancyDescriptionHtml_strips_disallowed_color_and_script()
    {
        var html = "<p><span style=\"color:#ff00ff\">X</span></p><script>alert(1)</script>";
        var safe = HtmlSanitize.ToVacancyDescriptionHtml(html);

        Assert.DoesNotContain("script", safe, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#ff00ff", safe, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<span>", safe);
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
