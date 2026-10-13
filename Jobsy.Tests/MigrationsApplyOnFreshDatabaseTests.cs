using System.Text.RegularExpressions;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Jobsy.Tests;

/// <summary>
/// Guards against migrations that apply cleanly on their own branch but collide after merges
/// (e.g. 20260930130923_AddOneTimeLinks re-adding Users.SchoolId from AddScholenFoundation,
/// which crash-looped the acceptatie API with Postgres 42701).
/// </summary>
public sealed partial class MigrationsApplyOnFreshDatabaseTests
{
    /// <summary>
    /// Applies every migration to a brand-new database on the configured Postgres server
    /// (CI: the PostGIS service in pr-tests.yml). Soft-skips when no Postgres is reachable.
    /// </summary>
    [Fact]
    public async Task All_migrations_apply_on_a_fresh_database()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(baseConnection);
        var databaseName = $"jobsy_migcheck_{Guid.NewGuid():N}";
        var admin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres", Pooling = false };
        try
        {
            await using var probe = new NpgsqlConnection(admin.ConnectionString);
            await probe.OpenAsync();
        }
        catch (Exception ex) when (ex is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
        {
            return;
        }

        await ExecuteAdminAsync(admin.ConnectionString, $"CREATE DATABASE \"{databaseName}\"");
        try
        {
            builder.Database = databaseName;
            builder.Pooling = false;
            var services = new ServiceCollection();
            services.AddDbContext<JobsyDbContext>(o =>
                o.UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseNetTopologySuite()));
            await using var sp = services.BuildServiceProvider();
            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                await db.Database.MigrateAsync();
                var pending = await db.Database.GetPendingMigrationsAsync();
                Assert.Empty(pending);
                Assert.False(db.Database.HasPendingModelChanges());
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAdminAsync(admin.ConnectionString, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
        }
    }

    /// <summary>
    /// Static scan of every migration's Up(): a column, table, index or foreign key may only be
    /// created once unless an earlier migration dropped or renamed it in between.
    /// </summary>
    [Fact]
    public void No_migration_recreates_schema_that_an_earlier_migration_already_created()
    {
        var dir = Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Data", "Migrations");
        var files = Directory.GetFiles(dir, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal)
                        && !f.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(files);

        var columns = new HashSet<string>(StringComparer.Ordinal);
        var tables = new HashSet<string>(StringComparer.Ordinal);
        var indexes = new HashSet<string>(StringComparer.Ordinal);
        var foreignKeys = new HashSet<string>(StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (var file in files)
        {
            var migration = Path.GetFileNameWithoutExtension(file);
            var up = ExtractUp(File.ReadAllText(file));
            foreach (Match op in OperationRegex().Matches(up))
            {
                var kind = op.Groups["op"].Value;
                var args = op.Groups["args"].Value;
                var name = Arg(args, "name");
                var table = Arg(args, "table");
                switch (kind)
                {
                    case "CreateTable" when name is not null:
                        if (!tables.Add(name))
                        {
                            problems.Add($"{migration}: CreateTable {name} already exists");
                        }

                        break;
                    case "DropTable" when name is not null:
                        tables.Remove(name);
                        columns.RemoveWhere(c => c.StartsWith(name + ".", StringComparison.Ordinal));
                        indexes.RemoveWhere(i => i.StartsWith("IX_" + name + "_", StringComparison.Ordinal));
                        foreignKeys.RemoveWhere(k => k.StartsWith("FK_" + name + "_", StringComparison.Ordinal));
                        break;
                    case "RenameTable" when name is not null:
                        var newTable = Arg(args, "newName");
                        tables.Remove(name);
                        if (newTable is not null)
                        {
                            tables.Add(newTable);
                            foreach (var c in columns.Where(c => c.StartsWith(name + ".", StringComparison.Ordinal)).ToList())
                            {
                                columns.Remove(c);
                                columns.Add(newTable + c[name.Length..]);
                            }
                        }

                        break;
                    case "AddColumn" when name is not null && table is not null:
                        if (!columns.Add($"{table}.{name}"))
                        {
                            problems.Add($"{migration}: AddColumn {table}.{name} already added by an earlier migration");
                        }

                        break;
                    case "DropColumn" when name is not null && table is not null:
                        columns.Remove($"{table}.{name}");
                        break;
                    case "RenameColumn" when name is not null && table is not null:
                        columns.Remove($"{table}.{name}");
                        var newColumn = Arg(args, "newName");
                        if (newColumn is not null)
                        {
                            columns.Add($"{table}.{newColumn}");
                        }

                        break;
                    case "CreateIndex" when name is not null:
                        if (!indexes.Add(name))
                        {
                            problems.Add($"{migration}: CreateIndex {name} already exists");
                        }

                        break;
                    case "DropIndex" when name is not null:
                        indexes.Remove(name);
                        break;
                    case "RenameIndex" when name is not null:
                        indexes.Remove(name);
                        if (Arg(args, "newName") is { } newIndex)
                        {
                            indexes.Add(newIndex);
                        }

                        break;
                    case "AddForeignKey" when name is not null:
                        if (!foreignKeys.Add(name))
                        {
                            problems.Add($"{migration}: AddForeignKey {name} already exists");
                        }

                        break;
                    case "DropForeignKey" when name is not null:
                        foreignKeys.Remove(name);
                        break;
                }
            }

            // Indexes and FKs declared inside CreateTable (constraints) are tracked by name too.
            foreach (Match fk in InlineForeignKeyRegex().Matches(up))
            {
                foreignKeys.Add(fk.Groups["name"].Value);
            }
        }

        Assert.True(problems.Count == 0, "Duplicate schema operations:\n" + string.Join("\n", problems));
    }

    private static async Task ExecuteAdminAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string ExtractUp(string source)
    {
        var start = source.IndexOf("void Up(", StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var end = source.IndexOf("void Down(", start, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..end];
    }

    private static string? Arg(string args, string key)
    {
        var match = Regex.Match(args, $@"\b{key}:\s*""(?<v>[^""]+)""");
        return match.Success ? match.Groups["v"].Value : null;
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    // Top-level migrationBuilder.X(...) calls; args captured up to the first "name/table/newName" block.
    [GeneratedRegex(@"migrationBuilder\.(?<op>CreateTable|DropTable|RenameTable|AddColumn|DropColumn|RenameColumn|CreateIndex|DropIndex|RenameIndex|AddForeignKey|DropForeignKey)(?:<[^>]+>)?\(\s*(?<args>(?:name|table|newName|schema|newSchema|oldName)\s*:\s*[^,\)]+(?:,\s*(?:name|table|newName|schema|newSchema|oldName)\s*:\s*[^,\)]+)*)", RegexOptions.Singleline)]
    private static partial Regex OperationRegex();

    [GeneratedRegex(@"table\.ForeignKey\(\s*name:\s*""(?<name>[^""]+)""", RegexOptions.Singleline)]
    private static partial Regex InlineForeignKeyRegex();
}
