using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class PendingModelChangesTests
{
    [Fact]
    public void Model_matches_snapshot_without_pending_changes()
    {
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o =>
            o.UseNpgsql(
                "Host=127.0.0.1;Database=JobsyPendingCheck;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite()));
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        // Compares runtime model to the compiled snapshot without needing a live database.
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
