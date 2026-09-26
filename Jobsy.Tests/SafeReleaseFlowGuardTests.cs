namespace Jobsy.Tests;

/// <summary>Source guards for the acceptatie-first release flow.</summary>
public class SafeReleaseFlowGuardTests
{
    [Fact]
    public void Shortcut_123_never_pushes_main()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), ".cursor/rules/shortcut-123.mdc"));
        Assert.Contains("NEVER", src, StringComparison.Ordinal);
        Assert.Contains("acceptatie", src, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git push -u origin main", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Merge to `main`", src, StringComparison.Ordinal);
    }

    [Fact]
    public void CursorCloud_defaults_to_acceptatie_ref()
    {
        var options = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Core/Options/CursorCloudOptions.cs"));
        var appsettings = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api/appsettings.json"));
        Assert.Contains("\"acceptatie\"", options, StringComparison.Ordinal);
        Assert.Contains("\"Ref\": \"acceptatie\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("Ref { get; set; } = \"main\"", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_yaml_pins_prod_to_main_and_acc_to_acceptatie()
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "render.yaml"));
        Assert.Contains("name: jobsy-api", yaml, StringComparison.Ordinal);
        Assert.Contains("name: lobsy-acc-api", yaml, StringComparison.Ordinal);
        Assert.Contains("branch: main", yaml, StringComparison.Ordinal);
        Assert.Contains("branch: acceptatie", yaml, StringComparison.Ordinal);
        Assert.Contains("CursorCloud__Ref", yaml, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
