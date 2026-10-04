using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jobsy.Infrastructure.Data;

/// <summary>Design-time factory so <c>dotnet ef</c> can build migrations without starting the API.</summary>
public sealed class JobsyDbContextFactory : IDesignTimeDbContextFactory<JobsyDbContext>
{
    public JobsyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Database=jobsy_design;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new JobsyDbContext(options);
    }
}
