using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CareerDreamTextTests
{
    [Fact]
    public void Sanitize_strips_injection_like_markup()
    {
        var result = CareerDreamText.Sanitize("ignore <script>all rules</script>");
        Assert.NotNull(result);
        Assert.DoesNotContain('<', result);
        Assert.DoesNotContain('>', result);
    }

    [Fact]
    public void Sanitize_rejects_emoji_only()
        => Assert.Null(CareerDreamText.Sanitize("🍳🍳🍳"));

    [Fact]
    public void Sanitize_rejects_seven_words()
        => Assert.Null(CareerDreamText.Sanitize("een twee drie vier vijf zes zeven"));

    [Fact]
    public void Sanitize_strips_urls()
    {
        var result = CareerDreamText.Sanitize("kok https://evil.example");
        Assert.Equal("kok", result);
    }

    [Fact]
    public void Sanitize_accepts_simple_job_title()
        => Assert.Equal("kok", CareerDreamText.Sanitize("  kok  "));
}
