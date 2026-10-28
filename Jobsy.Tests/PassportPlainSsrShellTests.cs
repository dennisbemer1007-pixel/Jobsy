namespace Jobsy.Tests;

public sealed class PassportPlainSsrShellTests
{
    [Fact]
    public void Plain_paspoort_shell_uses_mockup_878_markup()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var plain = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportPlainSsrShells.cs"));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/candidate-plain-paspoort.css"));
        Assert.Contains("cp-plain878", plain, StringComparison.Ordinal);
        Assert.Contains("<main class=\\\"wrap\\\">", plain, StringComparison.Ordinal);
        Assert.Contains("class=\\\"step\\\"", plain, StringComparison.Ordinal);
        Assert.Contains("class=\\\"grid\\\"", plain, StringComparison.Ordinal);
        Assert.Contains(".cp-plain878", css, StringComparison.Ordinal);
        Assert.Contains(".btn{", css, StringComparison.Ordinal);
        Assert.DoesNotContain("PassportSsrHeroHtml", plain, StringComparison.Ordinal);
    }
}
