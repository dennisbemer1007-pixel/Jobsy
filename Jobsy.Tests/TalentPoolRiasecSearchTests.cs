using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Tests;

public class TalentPoolRiasecSearchTests
{
    private static readonly Guid CompanyId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid SocialOnlyId = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    private static readonly Guid CompetencyId = Guid.Parse("a1000000-0000-0000-0000-000000000003");
    private static readonly Guid SocialAsMatchTagId = Guid.Parse("a1000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task Off_ignores_riasec_codes_and_hides_labels()
    {
        await using var db = await SeedAsync();
        var sut = Service(db, showRiasec: false);

        var bySocial = await sut.SearchAsync(CompanyId, new TalentPoolSearchQuery([CareerTestCatalog.Social]));
        Assert.Empty(bySocial);

        var mixed = await sut.SearchAsync(
            CompanyId,
            new TalentPoolSearchQuery([CareerTestCatalog.Social, "Samenwerken"]));
        Assert.Equal([CompetencyId], mixed.Select(c => c.CandidateUserId).ToArray());
        Assert.Empty(mixed[0].RiasecTags);
        Assert.Null(mixed[0].HollandCode);
        Assert.Contains("Samenwerken", mixed[0].MatchTags);

        var byCompetency = await sut.SearchAsync(CompanyId, new TalentPoolSearchQuery(["Samenwerken"]));
        Assert.Equal([CompetencyId], byCompetency.Select(c => c.CandidateUserId).ToArray());
        Assert.Empty(byCompetency[0].RiasecTags);
        Assert.Null(byCompetency[0].HollandCode);
    }

    [Fact]
    public async Task Off_does_not_match_a_riasec_code_that_is_also_a_match_tag()
    {
        await using var db = await SeedAsync();
        var sut = Service(db, showRiasec: false);

        var hits = await sut.SearchAsync(CompanyId, new TalentPoolSearchQuery(["social"]));
        Assert.DoesNotContain(hits, c => c.CandidateUserId == SocialAsMatchTagId);
        Assert.Empty(hits);
    }

    [Fact]
    public async Task On_keeps_riasec_matching_and_labels()
    {
        await using var db = await SeedAsync();
        var sut = Service(db, showRiasec: true);

        var bySocial = await sut.SearchAsync(CompanyId, new TalentPoolSearchQuery([CareerTestCatalog.Social]));
        var ids = bySocial.Select(c => c.CandidateUserId).ToHashSet();
        Assert.Contains(SocialOnlyId, ids);
        Assert.Contains(CompetencyId, ids);
        Assert.Contains(SocialAsMatchTagId, ids);

        var labeled = bySocial.Single(c => c.CandidateUserId == CompetencyId);
        Assert.Contains(CareerTestCatalog.Social, labeled.RiasecTags);
        Assert.Equal("S", labeled.HollandCode);
        Assert.Contains("Samenwerken", labeled.MatchTags);
    }

    [Fact]
    public async Task Missing_config_behaves_as_off()
    {
        await using var db = await SeedAsync();
        var sut = new TalentPoolService(db, null!, null!, null!, null!);

        var hits = await sut.SearchAsync(CompanyId, new TalentPoolSearchQuery([CareerTestCatalog.Social]));
        Assert.Empty(hits);
    }

    private static TalentPoolService Service(JobsyDbContext db, bool showRiasec)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TalentPoolRiasecVisibility.ConfigKey] = showRiasec ? "true" : "false"
            })
            .Build();
        return new TalentPoolService(db, null!, null!, null!, null!, configuration: config);
    }

    private static async Task<JobsyDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new JobsyDbContext(options);
        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "Westland BV",
            KvkNumber = "12345678",
            Address = "Laan 1",
            Location = new GeoPoint(52.0, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified
        });

        AddCandidate(db, SocialOnlyId, "social-only@jobsy.local", matchTags: [], riasec: [CareerTestCatalog.Social], holland: "S");
        AddCandidate(db, CompetencyId, "both@jobsy.local", matchTags: ["Samenwerken"], riasec: [CareerTestCatalog.Social], holland: "S");
        AddCandidate(db, SocialAsMatchTagId, "match-social@jobsy.local", matchTags: [CareerTestCatalog.Social], riasec: [], holland: "");
        await db.SaveChangesAsync();
        return db;
    }

    private static void AddCandidate(
        JobsyDbContext db,
        Guid id,
        string email,
        IReadOnlyList<string> matchTags,
        IReadOnlyList<string> riasec,
        string holland)
    {
        db.Users.Add(new User
        {
            Id = id,
            Email = email,
            FullName = "Anoniem",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            DateOfBirth = new DateOnly(1995, 4, 1),
            TalentPoolConsentAt = DateTime.UtcNow,
            TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
        });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = id,
            Status = CandidateCompetencyStatuses.Completed,
            HollandCode = holland,
            RiasecTagsJson = CareerTestCatalog.SerializeTags(riasec),
            MatchTagsJson = CareerTestCatalog.SerializeTags(matchTags),
            CompletedAtUtc = DateTime.UtcNow
        });
    }
}
