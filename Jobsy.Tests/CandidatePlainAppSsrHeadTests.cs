namespace Jobsy.Tests;

public sealed class CandidatePlainAppSsrHeadTests
{
    [Fact]
    public void App_razor_links_plain_css_for_paspoort_and_banenkaart()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        var resolver = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Features/CandidatePlainPublicResolver.cs"));
        Assert.Contains("IncludeCandidatePlainAppCss", app, StringComparison.Ordinal);
        Assert.Contains("candidate-plain-app.css", app, StringComparison.Ordinal);
        Assert.Contains("IncludeCandidatePlainPassportCss", app, StringComparison.Ordinal);
        Assert.Contains("candidate-plain-paspoort.css", app, StringComparison.Ordinal);
        Assert.Contains("IncludeCandidatePlainBanenkaartCss", app, StringComparison.Ordinal);
        Assert.Contains("candidate-plain-banenkaart.css", app, StringComparison.Ordinal);
        Assert.Contains("IsPlainCandidateAppPath", resolver, StringComparison.Ordinal);
        Assert.Contains("/candidate/paspoort", resolver, StringComparison.Ordinal);
        Assert.Contains("\"/banenkaart\"", resolver, StringComparison.Ordinal);
        Assert.Contains("\"/banen\"", resolver, StringComparison.Ordinal);
        Assert.Contains("/candidate/applications", resolver, StringComparison.Ordinal);
        Assert.Contains("/candidate/liked", resolver, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_paspoort_shell_skips_kreeft_strip()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var passport = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrShells.cs"));
        var passportPlain = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportPlainSsrShells.cs"));
        Assert.Contains("PassportPlainSsrShells.BuildBody", passport, StringComparison.Ordinal);
        Assert.Contains("cp-plain878", passportPlain, StringComparison.Ordinal);
        Assert.Contains("plainShell", passport, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_banenkaart_ssr_skips_empty_chrome_and_mockup_top()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Banenkaart/BanenkaartSsrShells.cs"));
        Assert.Contains("if (!plainShell)", shells, StringComparison.Ordinal);
        Assert.Contains("kb-banenkaart-ssr-chrome", shells, StringComparison.Ordinal);
        Assert.Contains("!model.IsAuthenticated && !plainShell", shells, StringComparison.Ordinal);
        Assert.Contains("kb-match-button__label", shells, StringComparison.Ordinal);
    }
}
