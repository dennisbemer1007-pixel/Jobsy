namespace Jobsy.Tests;

public sealed class CandidatePlainAppSsrHeadTests
{
    [Fact]
    public void App_razor_links_plain_app_css_for_paspoort_and_banenkaart()
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
        Assert.Contains("/banenkaart", resolver, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_app_shells_skip_kreeft_strips()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var passport = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrShells.cs"));
        var passportPlain = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportPlainSsrShells.cs"));
        var banenkaart = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Banenkaart/BanenkaartSsrShells.cs"));
        Assert.Contains("PassportPlainSsrShells.BuildBody", passport, StringComparison.Ordinal);
        Assert.Contains("cp-plain878", passportPlain, StringComparison.Ordinal);
        Assert.Contains("plainShell", passport, StringComparison.Ordinal);
        Assert.Contains("data-plain-banenkaart", banenkaart, StringComparison.Ordinal);
        Assert.Contains("cp-plain-banenkaart", banenkaart, StringComparison.Ordinal);
        Assert.Contains("plainShell", banenkaart, StringComparison.Ordinal);
    }
}
