using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Infrastructure.Scholen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Api.Ops;

/// <summary>
/// Acceptatie-only CLI: <c>dotnet Jobsy.Api.dll test-accounts seed|cleanup|status</c>.
/// No HTTP surface. Never prints passwords.
/// </summary>
public static class TestAccountsCommand
{
    public const int ExitOk = 0;
    public const int ExitUsage = 1;
    public const int ExitGuard = 2;
    public const int ExitPartial = 3;
    public const int ExitFailed = 4;

    public static async Task<int> RunAsync(string[] args, TextWriter? output = null)
    {
        output ??= Console.Out;
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp(output);
            return args.Length == 0 ? ExitUsage : ExitOk;
        }

        var verb = args[0].ToLowerInvariant();
        var options = args.Skip(1).ToArray();

        using var host = BuildCliHost();
        await host.StartAsync();
        try
        {
            using var scope = host.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var config = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IHostEnvironment>();
            var db = sp.GetRequiredService<JobsyDbContext>();

            var input = TestAccountGuardInputFactory.FromConfiguration(
                config, env, isWebRuntime: false);
            var guard = TestAccountEnvironmentGuard.Evaluate(input);
            if (!guard.Allowed)
            {
                await output.WriteLineAsync($"Refused: {guard.CodesSummary()}");
                return ExitGuard;
            }

            return verb switch
            {
                "status" => await RunStatusAsync(db, guard, output, CancellationToken.None),
                "seed" => await RunSeedAsync(sp, db, options, output, CancellationToken.None),
                "cleanup" => await RunCleanupAsync(sp, db, options, output, CancellationToken.None),
                _ => Usage(output, $"Unknown verb '{verb}'.")
            };
        }
        finally
        {
            await host.StopAsync();
        }
    }

    internal static IHost BuildCliHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
        builder.Services.RemoveAll<IHostedService>();
        return builder.Build();
    }

    private static async Task<int> RunStatusAsync(
        JobsyDbContext db,
        TestAccountGuardResult guard,
        TextWriter output,
        CancellationToken ct)
    {
        await output.WriteLineAsync($"Guard: allowed ({guard.CodesSummary()})");
        var users = await db.Users.AsNoTracking()
            .Where(u => u.IsTestAccount)
            .OrderBy(u => u.Email)
            .Select(u => new { u.Email, Role = u.Role.ToString(), u.LastLoginAtUtc })
            .ToListAsync(ct);
        await output.WriteLineAsync($"Test accounts: {users.Count}");
        foreach (var u in users)
        {
            var last = u.LastLoginAtUtc?.ToString("u") ?? "-";
            await output.WriteLineAsync($"  {u.Email,-40} {u.Role,-20} lastLogin={last}");
        }

        return ExitOk;
    }

    private static async Task<int> RunSeedAsync(
        IServiceProvider sp,
        JobsyDbContext db,
        string[] options,
        TextWriter output,
        CancellationToken ct)
    {
        var dryRun = options.Any(o => o.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
        HashSet<string>? only = null;
        for (var i = 0; i < options.Length; i++)
        {
            if (options[i].Equals("--only", StringComparison.OrdinalIgnoreCase)
                && i + 1 < options.Length)
            {
                only = options[i + 1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }

        if (await db.Database.GetPendingMigrationsAsync(ct) is { } pending && pending.Any())
        {
            await output.WriteLineAsync("Run the API once so migrations apply.");
            return ExitFailed;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        using var scope = TestAccountScope.Enter();
        try
        {
            var pupilCodes = sp.GetService<IPupilCodeService>();
            var seeder = new TestAccountsSeedService(db, sp.GetRequiredService<IConfiguration>(), pupilCodes);
            var result = await seeder.SeedAsync(dryRun, only, ct);

            if (result.AdminRealAccountConflict)
            {
                await tx.RollbackAsync(ct);
                await output.WriteLineAsync("Refused: Admin key collides with a real account.");
                PrintTable(output, result);
                return ExitFailed;
            }

            if (dryRun)
            {
                await tx.RollbackAsync(ct);
                await output.WriteLineAsync("Dry-run: no changes written.");
                PrintTable(output, result);
                return result.AnySkipped ? ExitPartial : ExitOk;
            }

            await db.SaveChangesAsync(ct);
            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "TestAccounts",
                Message = "seed",
                DetailsJson =
                    $"{{\"created\":{result.Created},\"updated\":{result.Updated},\"unchanged\":{result.Unchanged},\"skipped\":{result.Skipped},\"actor\":\"cli\"}}",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            try
            {
                sp.GetService<IVacancyDiscoveryIndex>()?.Invalidate();
                await output.WriteLineAsync(
                    "Banenkaart index invalidated; running API refreshes within ~15s.");
            }
            catch
            {
                // optional
            }

            PrintTable(output, result);
            return result.AnySkipped ? ExitPartial : ExitOk;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            await output.WriteLineAsync($"Failed: {ex.GetType().Name}");
            return ExitFailed;
        }
    }

    private static async Task<int> RunCleanupAsync(
        IServiceProvider sp,
        JobsyDbContext db,
        string[] options,
        TextWriter output,
        CancellationToken ct)
    {
        var execute = options.Any(o => o.Equals("--execute", StringComparison.OrdinalIgnoreCase));
        int? expectUsers = null;
        for (var i = 0; i < options.Length; i++)
        {
            if (options[i].Equals("--expect-users", StringComparison.OrdinalIgnoreCase)
                && i + 1 < options.Length
                && int.TryParse(options[i + 1], out var n))
            {
                expectUsers = n;
            }
        }

        if (await db.Database.GetPendingMigrationsAsync(ct) is { } pending && pending.Any())
        {
            await output.WriteLineAsync("Run the API once so migrations apply.");
            return ExitFailed;
        }

        var cleanup = new TestAccountsCleanupService(db);
        var plan = await cleanup.PlanAsync(ct);
        await output.WriteLineAsync($"Test users: {plan.TestUserCount}");
        foreach (var (email, role) in plan.Users)
        {
            await output.WriteLineAsync($"  {email,-40} {role}");
        }

        await output.WriteLineAsync("Row counts:");
        foreach (var (table, count) in plan.TableCounts.OrderBy(kv => kv.Key))
        {
            await output.WriteLineAsync($"  {table,-28} {count}");
        }

        if (!execute)
        {
            await output.WriteLineAsync(
                $"To delete: dotnet Jobsy.Api.dll test-accounts cleanup --execute --expect-users {plan.TestUserCount}");
            return ExitOk;
        }

        if (expectUsers is null)
        {
            return Usage(output, "--execute requires --expect-users <n>.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        using var scope = TestAccountScope.Enter();
        try
        {
            var (ok, error) = await cleanup.ExecuteAsync(expectUsers.Value, ct);
            if (!ok)
            {
                await tx.RollbackAsync(ct);
                await output.WriteLineAsync($"Refused: {error}");
                return error?.Contains("expect-users", StringComparison.Ordinal) == true
                    ? ExitUsage
                    : ExitPartial;
            }

            db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "TestAccounts",
                Message = "cleanup",
                DetailsJson = $"{{\"deletedUsers\":{expectUsers.Value},\"actor\":\"cli\"}}",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            try
            {
                sp.GetService<IVacancyDiscoveryIndex>()?.Invalidate();
            }
            catch
            {
                // optional
            }

            await output.WriteLineAsync($"Deleted {expectUsers.Value} test users and related test data.");
            return ExitOk;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            await output.WriteLineAsync($"Failed: {ex.GetType().Name}");
            return ExitFailed;
        }
    }

    private static void PrintTable(TextWriter output, TestAccountsSeedResult result)
    {
        output.WriteLine("AccountKey         Email                                      Role                 Action     Reason");
        output.WriteLine(new string('-', 120));
        foreach (var row in result.Rows)
        {
            output.WriteLine(
                $"{row.AccountKey,-18} {row.Email,-42} {row.Role,-20} {row.Action,-10} {row.Reason}");
        }
    }

    private static int Usage(TextWriter output, string message)
    {
        output.WriteLine(message);
        PrintHelp(output);
        return ExitUsage;
    }

    private static bool IsHelp(string arg)
        => arg is "-h" or "--help" or "help";

    private static void PrintHelp(TextWriter output)
    {
        output.WriteLine("Usage: dotnet Jobsy.Api.dll test-accounts <seed|cleanup|status> [options]");
        output.WriteLine("  seed [--dry-run] [--only <AccountKey,...>]");
        output.WriteLine("  cleanup [--execute --expect-users <n>]");
        output.WriteLine("  status");
        output.WriteLine("Acceptatie only. Passwords from TestAccounts__Password__<Key> env vars.");
    }
}
