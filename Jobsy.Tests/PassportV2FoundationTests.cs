using Jobsy.Api.Controllers;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class WorkPreferenceCatalogTests
{
    [Theory]
    [InlineData("PREFER", "prefer")]
    [InlineData(" ok ", "ok")]
    [InlineData("nope", null)]
    public void Indoor_canonicalizes_known_codes(string raw, string? expected)
    {
        Assert.Equal(expected, WorkPreferenceCatalogs.CanonicalIndoor(raw));
    }

    [Fact]
    public void Contracts_drop_unknown_keep_geen_voorkeur_exclusive_and_cap_at_three()
    {
        var mixed = WorkPreferenceCatalogs.NormalizeContracts(
            ["VAST", "nope", "uitzend", "vast", "tijdelijk", "seizoen"]);
        Assert.Equal(["vast", "uitzend", "tijdelijk"], mixed);

        var exclusive = WorkPreferenceCatalogs.NormalizeContracts(["vast", "geen-voorkeur", "oproep"]);
        Assert.Equal(["geen-voorkeur"], exclusive);
    }
}

public class WorkRegionRulesTests
{
    [Fact]
    public void Sanitize_strips_digits_collapses_space_and_caps_at_60()
    {
        var home = "Kerkstraat 12, 3511 AB Utrecht";
        var stored = WorkRegionRules.Sanitize(home);
        Assert.NotNull(stored);
        Assert.DoesNotContain("3511", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("12", stored, StringComparison.Ordinal);
        Assert.False(WorkRegionRules.IsVerbatimHomeAddress(stored, home));
        Assert.True(stored.Length <= WorkRegionRules.MaxLength);

        var longCity = new string('a', 80);
        var capped = WorkRegionRules.Sanitize(longCity);
        Assert.NotNull(capped);
        Assert.Equal(60, capped.Length);
    }

    [Fact]
    public void Suggestion_is_city_area_and_is_not_the_home_address()
    {
        var home = "Kerkstraat 12, 3511 AB Utrecht";
        var suggestion = WorkRegionRules.SuggestFromHomeAddress(home);
        Assert.Equal("Utrecht e.o.", suggestion);
        Assert.False(WorkRegionRules.IsVerbatimHomeAddress(suggestion, home));
    }
}

public class ShareablePreferenceRoundTripTests
{
    [Fact]
    public void Old_preferences_json_round_trips_with_null_shareable_defaults()
    {
        const string oldJson = """{"roles":["magazijn"],"maxTravelMinutes":30,"homeAddress":"Kerkstraat 12, 3511 AB Utrecht"}""";
        var parsed = MeController.ParsePreferences(oldJson);
        Assert.Equal("magazijn", Assert.Single(parsed.Roles));
        Assert.Equal(30, parsed.MaxTravelMinutes);
        Assert.Null(parsed.WorkPreferences);
        Assert.Null(parsed.ShareEmployerPreferences);
        Assert.Null(parsed.WorkRegion);
        Assert.Null(parsed.HasOwnCar);
        Assert.Null(parsed.ContractPreferences);
        Assert.False(WorkRegionRules.IsVerbatimHomeAddress(parsed.WorkRegion, parsed.HomeAddress));

        var again = MeController.ParsePreferences(MeController.SerializePreferences(parsed));
        Assert.Null(again.WorkPreferences);
        Assert.Null(again.WorkRegion);
        Assert.Equal(parsed.HomeAddress, again.HomeAddress);
    }

    [Fact]
    public void New_fields_round_trip_and_unknown_codes_are_dropped()
    {
        var saved = MeController.SerializePreferences(new CandidatePreferencesDto(
            ["magazijn"],
            20,
            null,
            WorkPreferences: new SharedWorkPreferences("PREFER", "nope", "lifting-15", "calm"),
            ShareEmployerPreferences: true,
            WorkRegion: "3511 AB Utrecht",
            HasOwnCar: false,
            ContractPreferences: ["vast", "geen-voorkeur", "ghost"]));

        var parsed = MeController.ParsePreferences(saved);
        Assert.Equal("prefer", parsed.WorkPreferences?.Indoor);
        Assert.Null(parsed.WorkPreferences?.Outdoor);
        Assert.Equal("lifting-15", parsed.WorkPreferences?.PhysicalWork);
        Assert.Equal("calm", parsed.WorkPreferences?.Pace);
        Assert.True(parsed.ShareEmployerPreferences);
        Assert.Equal("AB Utrecht", parsed.WorkRegion);
        Assert.False(parsed.HasOwnCar);
        Assert.Equal(["geen-voorkeur"], parsed.ContractPreferences);
    }

    [Fact]
    public void Null_incoming_shareable_fields_keep_the_stored_value()
    {
        var existing = new SharedWorkPreferences("ok", "prefer", null, null);
        Assert.Equal(existing, ShareablePreferenceNormalizer.NormalizeWork(null, existing));
        Assert.Equal("Utrecht e.o.", ShareablePreferenceNormalizer.NormalizeRegion(null, "Utrecht e.o."));
        Assert.Equal(["vast"], ShareablePreferenceNormalizer.NormalizeContracts(null, ["vast"]));
    }
}

public class ContactVerificationTests
{
    [Fact]
    public void Phone_change_clears_verification_same_number_keeps_it()
    {
        var stamped = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        var user = new User
        {
            Role = UserRole.Candidate,
            PhoneNumber = "06 12345678",
            PhoneVerifiedAtUtc = stamped,
            PhoneVerifiedE164 = "06 12345678"
        };

        ContactVerification.ApplyPhone(user, "06 12345678");
        Assert.Equal(stamped, user.PhoneVerifiedAtUtc);
        Assert.Equal("06 12345678", user.PhoneVerifiedE164);

        ContactVerification.ApplyPhone(user, "0687654321");
        Assert.Null(user.PhoneVerifiedAtUtc);
        Assert.Null(user.PhoneVerifiedE164);
        Assert.Equal("0687654321", user.PhoneNumber);
    }

    [Fact]
    public void Email_stamp_is_candidate_only_and_is_not_overwritten()
    {
        var first = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        var later = first.AddDays(1);
        var candidate = new User { Role = UserRole.Candidate };
        ContactVerification.MarkEmailVerified(candidate, first);
        ContactVerification.MarkEmailVerified(candidate, later);
        Assert.Equal(first, candidate.EmailVerifiedAtUtc);

        var employer = new User { Role = UserRole.BranchManager };
        ContactVerification.MarkEmailVerified(employer, first);
        Assert.Null(employer.EmailVerifiedAtUtc);
    }
}

public class PhoneVerificationLockoutTests
{
    [Fact]
    public async Task Disabled_flag_does_not_start_a_challenge()
    {
        await using var db = CreateDb();
        var user = SeedUser(db, "0612345678");
        await db.SaveChangesAsync();
        var sut = new PhoneVerificationService(db, new FixedPlatformFeatures(phone: false), new CapturingSms());

        var result = await sut.StartAsync(user.Id);
        Assert.False(result.Ok);
        Assert.Equal("phone_verification_disabled", result.Error);
        Assert.Empty(db.PhoneVerificationChallenges);
    }

    [Fact]
    public async Task Wrong_code_locks_out_after_max_attempts_and_stores_only_a_hash()
    {
        await using var db = CreateDb();
        var user = SeedUser(db, "0612345678");
        await db.SaveChangesAsync();
        var sms = new CapturingSms();
        var sut = new PhoneVerificationService(db, new FixedPlatformFeatures(phone: true), sms);

        var start = await sut.StartAsync(user.Id);
        Assert.True(start.Ok);
        var code = sms.Bodies.Single()["Lobsy code: ".Length..];
        var challenge = await db.PhoneVerificationChallenges.SingleAsync();
        Assert.NotEqual(code, challenge.CodeHash);
        Assert.Equal(64, challenge.CodeHash.Length);

        for (var i = 0; i < 4; i++)
        {
            var wrong = await sut.VerifyAsync(user.Id, start.ChallengeId!.Value, "000000");
            Assert.Equal("invalid_code", wrong.Error);
        }

        var burned = await sut.VerifyAsync(user.Id, start.ChallengeId!.Value, "000000");
        Assert.Equal("code_expired", burned.Error);

        var after = await sut.VerifyAsync(user.Id, start.ChallengeId!.Value, code);
        Assert.Equal("code_expired", after.Error);
        Assert.Null(user.PhoneVerifiedAtUtc);
    }

    [Fact]
    public async Task Matching_code_stamps_the_verified_number()
    {
        await using var db = CreateDb();
        var user = SeedUser(db, "0612345678");
        await db.SaveChangesAsync();
        var sms = new CapturingSms();
        var sut = new PhoneVerificationService(db, new FixedPlatformFeatures(phone: true), sms);

        var start = await sut.StartAsync(user.Id);
        var code = sms.Bodies.Single()["Lobsy code: ".Length..];
        var ok = await sut.VerifyAsync(user.Id, start.ChallengeId!.Value, code);
        Assert.True(ok.Ok);
        Assert.NotNull(user.PhoneVerifiedAtUtc);
        Assert.Equal("0612345678", user.PhoneVerifiedE164);
    }

    private static User SeedUser(JobsyDbContext db, string phone)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"phone-{Guid.NewGuid():N}@example.com",
            FullName = "Phone",
            Role = UserRole.Candidate,
            IsActive = true,
            PhoneNumber = phone
        };
        db.Users.Add(user);
        return user;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingSms : ISmsSender
    {
        public List<string> Bodies { get; } = [];

        public Task SendAsync(string e164, string body, CancellationToken cancellationToken = default)
        {
            Bodies.Add(body);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedPlatformFeatures(bool phone) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false,
                false,
                "https://lobsy.test",
                null,
                PhoneVerificationEnabled: phone));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }
}

public class PrivatePreferencesGuardTests
{
    [Fact]
    public void Private_dto_is_not_used_by_passport_or_cv_builders()
    {
        var root = FindRepoRoot();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CandidateContracts.cs",
            "CandidatePrivatePreferences.cs",
            "CandidatePrivatePreferencesController.cs",
            "CandidatePrivatePreferencesValidator.cs",
            "IKbDislikeSource.cs"
        };

        var offenders = new List<string>();
        foreach (var project in new[] { "Jobsy.Core", "Jobsy.Api" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var name = Path.GetFileName(file);
                var text = File.ReadAllText(file);
                var mentionsPrivate = text.Contains("CandidatePrivatePreferences", StringComparison.Ordinal);
                var builder = name.Contains("Passport", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("LobsyCv", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("CvModel", StringComparison.OrdinalIgnoreCase);
                if (builder && mentionsPrivate)
                {
                    offenders.Add(file);
                }

                if (mentionsPrivate
                    && text.Contains("WorkPreferences", StringComparison.Ordinal)
                    && !allowed.Contains(name))
                {
                    offenders.Add(file + " maps private prefs next to WorkPreferences");
                }

                if (text.Contains("CandidatePrivatePreferencesDto", StringComparison.Ordinal)
                    && !allowed.Contains(name))
                {
                    offenders.Add(file);
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    internal static string FindRepoRoot()
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
}

public class PassportV2FoundationMigrationTests
{
    [Fact]
    public void Migration_adds_columns_backfills_proved_email_and_flags_default_false()
    {
        var dir = Path.Combine(PrivatePreferencesGuardTests.FindRepoRoot(), "Jobsy.Infrastructure", "Data", "Migrations");
        var file = Directory.GetFiles(dir, "*_AddPassportV2Foundation.cs")
            .Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var source = File.ReadAllText(file);
        Assert.Contains("EmailVerifiedAtUtc", source, StringComparison.Ordinal);
        Assert.Contains("PhoneVerifiedAtUtc", source, StringComparison.Ordinal);
        Assert.Contains("PhoneVerifiedE164", source, StringComparison.Ordinal);
        Assert.Contains("PassportPartnersEnabled", source, StringComparison.Ordinal);
        Assert.Contains("PassportPdfV2Enabled", source, StringComparison.Ordinal);
        Assert.Contains("PhoneVerificationEnabled", source, StringComparison.Ordinal);
        Assert.Contains("defaultValue: false", source, StringComparison.Ordinal);
        Assert.Contains("""WHERE "Role" = 0""", source, StringComparison.Ordinal);
        Assert.Contains(
            "\"EmailVerifiedAtUtc\" = COALESCE(\"LastLoginAtUtc\", \"TermsAcceptedAt\")",
            source,
            StringComparison.Ordinal);

        var downIdx = source.IndexOf("protected override void Down", StringComparison.Ordinal);
        Assert.True(downIdx > 0);
        var down = source[downIdx..];
        Assert.DoesNotContain("UPDATE", down, StringComparison.OrdinalIgnoreCase);

        var snapshot = File.ReadAllText(Path.Combine(dir, "JobsyDbContextModelSnapshot.cs"));
        foreach (var column in new[] { "PassportPartnersEnabled", "PassportPdfV2Enabled", "PhoneVerificationEnabled" })
        {
            var idx = snapshot.IndexOf($"\"{column}\"", StringComparison.Ordinal);
            Assert.True(idx > 0, column);
            var window = snapshot.Substring(idx, Math.Min(220, snapshot.Length - idx));
            Assert.Contains("HasDefaultValue(false)", window, StringComparison.Ordinal);
        }
    }
}
