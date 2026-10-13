using Jobsy.Api.Ops;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Jobsy.Tests.TestAccounts;

/// <summary>
/// Full <c>test-accounts seed</c> + <c>status</c> against PostgreSQL, in the acceptatie
/// Production host shape (the CLI path that failed on lobsy-acc-api).
/// </summary>
public sealed class TestAccountsPostgresSeedTests
{
    private const string Password = "Acceptatie-Seed-Test-1";
    private const string HmacKey = "test-hmac-key-for-seed";

    [Fact]
    public async Task Seed_and_status_survive_missing_hmac_and_a_second_run_is_unchanged()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

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

        var databaseName = $"lobsy_seed_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = databaseName,
            Pooling = false
        };
        await ExecuteAdminAsync(admin.ConnectionString, $"CREATE DATABASE \"{databaseName}\"");
        try
        {
            var services = new ServiceCollection();
            services.AddDbContext<JobsyDbContext>(o =>
                o.UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseNetTopologySuite()));
            await using var sp = services.BuildServiceProvider();
            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                await db.Database.MigrateAsync();
            }

            var withoutKey = AcceptatieConfig(builder.ConnectionString, databaseName, includeHmac: false);
            var first = await RunAsync(["seed"], withoutKey);
            Assert.Equal(TestAccountsCommand.ExitPartial, first.Code);
            Assert.Contains("Pupil codes skipped: Scholen:CodeHmacKey is not configured.", first.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed:", first.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(Password, first.Text, StringComparison.Ordinal);
            Assert.Equal(10, CountAction(first.Text, "Created"));
            Assert.Equal(1, CountAction(first.Text, "Skipped"));
            Assert.Contains("Ambassadeur", first.Text, StringComparison.Ordinal);

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                Assert.Equal(10, await db.Users.CountAsync(u => u.IsTestAccount));
                Assert.Equal(0, await db.PupilCodes.CountAsync());
                Assert.Equal(PupilQuestionSet.Vo, (await db.SchoolClasses.SingleAsync(c => c.Name == "1A")).QuestionSet);
                Assert.Equal(PupilQuestionSet.Groep78, (await db.SchoolClasses.SingleAsync(c => c.Name == "7A")).QuestionSet);
            }

            var withKey = AcceptatieConfig(builder.ConnectionString, databaseName, includeHmac: true);
            var second = await RunAsync(["seed"], withKey);
            Assert.Equal(TestAccountsCommand.ExitPartial, second.Code);
            Assert.DoesNotContain("Failed:", second.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(Password, second.Text, StringComparison.Ordinal);
            Assert.Equal(0, CountAction(second.Text, "Created"));
            Assert.Equal(0, CountAction(second.Text, "Updated"));
            Assert.Equal(10, CountAction(second.Text, "Unchanged"));
            Assert.Equal(1, CountAction(second.Text, "Skipped"));

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                Assert.Equal(10, await db.PupilCodes.CountAsync());
            }

            var third = await RunAsync(["seed"], withKey);
            Assert.Equal(TestAccountsCommand.ExitPartial, third.Code);
            Assert.Equal(0, CountAction(third.Text, "Created"));
            Assert.Equal(0, CountAction(third.Text, "Updated"));
            Assert.Equal(10, CountAction(third.Text, "Unchanged"));

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                Assert.Equal(10, await db.PupilCodes.CountAsync());
                Assert.Equal(10, await db.Users.CountAsync(u => u.IsTestAccount));
            }

            var status = await RunAsync(["status"], withKey);
            Assert.Equal(TestAccountsCommand.ExitOk, status.Code);
            Assert.Contains("Test accounts: 10", status.Text, StringComparison.Ordinal);
            Assert.Contains("test-admin@lobsy.nl", status.Text, StringComparison.Ordinal);
            Assert.Contains("test-leraar@lobsy.nl", status.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(Password, status.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed:", status.Text, StringComparison.Ordinal);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAdminAsync(admin.ConnectionString, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
        }
    }

    private static Dictionary<string, string?> AcceptatieConfig(string connectionString, string databaseName, bool includeHmac)
    {
        var config = new Dictionary<string, string?>
        {
            ["ConnectionStrings:JobsyDb"] = connectionString,
            ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
            ["TestAccounts:Enabled"] = "true",
            ["TestAccounts:EmailDomain"] = "lobsy.nl",
            ["TestAccounts:ExpectedDatabaseName"] = databaseName,
            ["RENDER_SERVICE_NAME"] = "lobsy-acc-api",
            ["PublicWebBaseUrl"] = "https://acceptatie.lobsy.nl",
            ["JobsyAuth:AllowStubPayments"] = "true",
            ["TestAccounts:Password:Candidate"] = Password,
            ["TestAccounts:Password:CandidateNew"] = Password,
            ["TestAccounts:Password:BranchManager"] = Password,
            ["TestAccounts:Password:EnterpriseManager"] = Password,
            ["TestAccounts:Password:RegionalManager"] = Password,
            ["TestAccounts:Password:Intermediary"] = Password,
            ["TestAccounts:Password:SalesManager"] = Password,
            ["TestAccounts:Password:Admin"] = Password,
            ["TestAccounts:Password:Teacher"] = Password,
            ["TestAccounts:Password:SchoolAdmin"] = Password
        };
        // Always set the key so a developer environment variable cannot leak into the host.
        config["Scholen:CodeHmacKey"] = includeHmac ? HmacKey : "";

        return config;
    }

    private static async Task<(int Code, string Text)> RunAsync(
        string[] args,
        IReadOnlyDictionary<string, string?> config)
    {
        var output = new StringWriter();
        var code = await TestAccountsCommand.RunAsync(
            args,
            output,
            hostEnvironmentName: "Production",
            configurationOverrides: config);
        return (code, output.ToString());
    }

    private static int CountAction(string output, string action)
        => output.Split('\n').Count(line =>
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 4 && parts[3] == action;
        });

    private static async Task ExecuteAdminAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
