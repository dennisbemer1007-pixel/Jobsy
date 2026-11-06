namespace Jobsy.Tests;

public sealed class FeatureFlagsActivityLogMappingTests
{
    [Fact]
    public void Api_feature_flags_cache_maps_activity_log_from_platform_snapshot()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Infrastructure/Features/FeatureFlags.cs"));
        Assert.Contains("ActivityLogEnabled: snap.ActivityLogEnabled", src, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
