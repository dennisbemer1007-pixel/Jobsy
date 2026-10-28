namespace Jobsy.Tests;

public sealed class CandidatePlainPublicSsrHeadTests
{
    [Fact]
    public void App_razor_links_plain_public_css_in_static_head_with_leading_slash()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("IncludeCandidatePlainPublicCss", app, StringComparison.Ordinal);
        Assert.Contains(
            "UrlLeadingSlash(\"css/features/candidate-plain-public.css\")",
            app,
            StringComparison.Ordinal);
        Assert.Contains("data-plain-public-css", app, StringComparison.Ordinal);
    }
}
