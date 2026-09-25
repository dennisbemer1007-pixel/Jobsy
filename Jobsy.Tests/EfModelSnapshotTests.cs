namespace Jobsy.Tests;

/// <summary>
/// Guards against hand-written migrations that skip updating JobsyDbContextModelSnapshot.
/// EF Core 9 treats pending model changes as a hard error during MigrateAsync, which stops the API host on Render.
/// </summary>
public class EfModelSnapshotTests
{
    [Fact]
    public void Model_snapshot_includes_web_push_subscriptions()
    {
        var snapshot = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure",
            "Data",
            "Migrations",
            "JobsyDbContextModelSnapshot.cs"));

        Assert.Contains("WebPushSubscription", snapshot, StringComparison.Ordinal);
        Assert.Contains("\"WebPushSubscriptions\"", snapshot, StringComparison.Ordinal);
        Assert.Contains("Jobsy.Core.Entities.WebPushSubscription", snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_push_migration_exists_for_subscriptions_table()
    {
        var migrationsDir = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure",
            "Data",
            "Migrations");
        var migration = Directory.EnumerateFiles(migrationsDir, "*AddWebPushSubscriptions.cs")
            .SingleOrDefault();
        Assert.NotNull(migration);
        var text = File.ReadAllText(migration!);
        Assert.Contains("CreateTable", text, StringComparison.Ordinal);
        Assert.Contains("WebPushSubscriptions", text, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate Jobsy.sln from test base directory.");
    }
}
