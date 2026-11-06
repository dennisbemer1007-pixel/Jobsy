using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Scholen;

public class PupilCodeServiceTests
{
    [Fact]
    public async Task Generate_10000_codes_are_well_formed_and_unique()
    {
        await using var db = CreateDb();
        var schoolClass = await SeedClassAsync(db);
        var svc = CreateService(db);

        // Generate in batches of 40 (max per call).
        var all = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 250; i++)
        {
            var batch = await svc.GenerateAsync(40, schoolClass);
            foreach (var row in batch)
            {
                var plain = svc.Unprotect(row.CodeProtected);
                Assert.NotNull(plain);
                Assert.True(PupilCodeFormat.IsWellFormed(plain));
                Assert.Equal(6, plain!.Length);
                Assert.All(plain, ch => Assert.Contains(ch, PupilCodeFormat.Alphabet));
                Assert.True(all.Add(row.CodeLookupHash));
            }
        }

        Assert.Equal(10_000, all.Count);
    }

    [Fact]
    public void NextUniquePlainCode_survives_fifty_plus_forced_collisions()
    {
        var alphabet = PupilCodeFormat.Alphabet.ToCharArray();
        Span<char> chars = stackalloc char[PupilCodeFormat.Length];
        var used = new HashSet<string>(StringComparer.Ordinal) { "collision" };
        var calls = 0;
        string Hash(string code)
        {
            calls++;
            // First 55 attempts keep colliding; then a unique hash.
            return calls <= 55 ? "collision" : "ok-" + code;
        }

        var code = PupilCodeService.NextUniquePlainCode(alphabet, chars, used, Hash, maxAttempts: 100);
        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.True(calls > 50, $"expected >50 collision retries, got {calls}");
        Assert.Contains(used, h => h.StartsWith("ok-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Replace_invalidates_old_hash_and_bumps_session()
    {
        await using var db = CreateDb();
        var schoolClass = await SeedClassAsync(db);
        var svc = CreateService(db);
        var created = await svc.GenerateAsync(1, schoolClass);
        var code = created[0];
        var oldHash = code.CodeLookupHash;
        var oldVersion = code.SessionVersion;
        var oldPlain = svc.Unprotect(code.CodeProtected)!;

        await svc.ReplaceAsync(code);
        Assert.NotEqual(oldHash, code.CodeLookupHash);
        Assert.Equal(oldVersion + 1, code.SessionVersion);
        var fresh = svc.Unprotect(code.CodeProtected)!;
        Assert.NotEqual(oldPlain, fresh);
        Assert.NotEqual(svc.LookupHash(oldPlain), code.CodeLookupHash);
    }

    private static async Task<SchoolClass> SeedClassAsync(JobsyDbContext db)
    {
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "T",
            City = "C",
            AllowedEmailDomains = "[]",
            CreatedAtUtc = DateTime.UtcNow
        };
        var sc = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "1A",
            Year = 1,
            SchoolYearStart = 2026,
            PupilCount = 40,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(sc);
        await db.SaveChangesAsync();
        return sc;
    }

    private static PupilCodeService CreateService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Scholen:CodeHmacKey"] = "test-hmac-key-for-scholen-unit-tests"
            })
            .Build();
        var env = new FakeHostEnvironment();
        var dp = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "scholen-dp-" + Guid.NewGuid()));
        return new PupilCodeService(db, dp, config, env);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("PupilCode-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
