namespace Jobsy.Tests;

public sealed class ReferenceVerificationHeroAssetTests
{
    [Fact]
    public void Ref_full_hero_assets_exist()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var desktop = Path.Combine(repoRoot, "Jobsy.Web/wwwroot/images/candidate/ref-full.jpg");
        var mobile = Path.Combine(repoRoot, "Jobsy.Web/wwwroot/images/candidate/ref-full-mobile.jpg");
        Assert.True(File.Exists(desktop), desktop);
        Assert.True(File.Exists(mobile), mobile);
        Assert.True(new FileInfo(desktop).Length > 50_000);
        Assert.True(new FileInfo(mobile).Length > 30_000);
    }
}
