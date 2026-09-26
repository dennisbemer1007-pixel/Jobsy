using System.Reflection;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jobsy.Tests;

/// <summary>
/// Manual migrations without <see cref="MigrationAttribute"/> are invisible to EF —
/// Acc then 500s on local-login when the snapshot expects columns that were never applied.
/// </summary>
public class EfMigrationDiscoveryTests
{
    [Fact]
    public void All_Migration_subclasses_carry_MigrationAttribute()
    {
        var migrationTypes = typeof(JobsyDbContext).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(Migration).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(migrationTypes);

        var missing = migrationTypes
            .Where(t => t.GetCustomAttribute<MigrationAttribute>() is null)
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Migrations missing [Migration(\"…\")] (EF will skip them): "
            + string.Join(", ", missing));
    }

    [Fact]
    public void LoginProtectionAndMfa_migration_id_is_registered()
    {
        var ids = typeof(JobsyDbContext).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(Migration).IsAssignableFrom(t))
            .Select(t => t.GetCustomAttribute<MigrationAttribute>()?.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("20260926170000_LoginProtectionAndMfa", ids);
    }
}
