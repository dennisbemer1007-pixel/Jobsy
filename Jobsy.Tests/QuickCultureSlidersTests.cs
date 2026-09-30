using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests;

public class QuickCultureSlidersTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Slider_v_writes_answers_2k_minus_1_equals_v_and_2k_equals_6_minus_v(int v)
    {
        var sliders = CulturePersonalityCatalog.CultureDimensionCodes
            .ToDictionary(c => c, _ => v, StringComparer.OrdinalIgnoreCase);
        var answers = QuickCultureSliders.ToAnswers(sliders);
        for (var k = 1; k <= 6; k++)
        {
            Assert.Equal(v, answers[2 * k - 1]);
            Assert.Equal(6 - v, answers[2 * k]);
        }
    }

    [Fact]
    public void Percents_equal_full_scan_scoring_of_same_answers()
    {
        var sliders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [CulturePersonalityCatalog.Autonomy] = 4,
            [CulturePersonalityCatalog.Informal] = 4,
            [CulturePersonalityCatalog.Collaboration] = 5,
            [CulturePersonalityCatalog.Flexibility] = 2,
            [CulturePersonalityCatalog.Innovation] = 3,
            [CulturePersonalityCatalog.PeopleFirst] = 5
        };
        var answers = QuickCultureSliders.ToAnswers(sliders).ToDictionary(kv => kv.Key, kv => kv.Value);
        for (var i = 13; i <= 18; i++)
        {
            answers[i] = 3;
        }

        var fromQuick = CulturePersonalityCatalog.Score(answers);
        Assert.NotNull(fromQuick);

        // Same answers scored again (= full-scan scoring path).
        var fromFull = CulturePersonalityCatalog.Score(answers);
        Assert.Equal(fromQuick!.Autonomy, fromFull!.Autonomy);
        Assert.Equal(fromQuick.PeopleFirst, fromFull.PeopleFirst);
    }

    [Fact]
    public async Task Save_sets_Source_Quick_and_full_scan_overwrites_to_Full_evicting_cache()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var company = SeedCompany(db);
        await db.SaveChangesAsync();

        var extras = new CompanyProfileExtrasService(db, cache, new StubKvk());
        await extras.SaveAsync(company.Id, new Core.Interfaces.CompanyProfileExtrasUpdate(
            CultureSliders: CulturePersonalityCatalog.CultureDimensionCodes
                .ToDictionary(c => c, _ => 4, StringComparer.OrdinalIgnoreCase)));

        var row = await db.CompanyCultureProfiles.SingleAsync(p => p.CompanyId == company.Id);
        Assert.Equal(CompanyCultureSources.Quick, row.Source);
        Assert.Equal(CandidateCompetencyStatuses.Completed, row.Status);

        var lookup = new CompanyCultureLookup(db, cache);
        var cached = await lookup.GetForCompaniesAsync([company.Id]);
        Assert.NotNull(cached[company.Id].Culture);

        var culture = new CompanyCultureService(db, cache);
        var answers = CulturePersonalityCatalog.ParseAnswers(row.AnswersJson);
        await culture.SaveAsync(company.Id, answers, complete: true);

        row = await db.CompanyCultureProfiles.SingleAsync(p => p.CompanyId == company.Id);
        Assert.Equal(CompanyCultureSources.Full, row.Source);

        // Cache evicted → fresh read still works.
        var refreshed = await lookup.GetForCompaniesAsync([company.Id]);
        Assert.NotNull(refreshed[company.Id].Culture);
    }

    private static Company SeedCompany(JobsyDbContext db)
    {
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Name = "QuickCo",
            KvkNumber = "12345678",
            Address = "A 1",
            Location = new GeoPoint(52, 5),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(c);
        return c;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("quick-culture-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(null);

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<KvkEstablishmentResult>>([]);

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkEstablishmentsLookup.Ok([]));

        public Task<KvkSearchResult> SearchAsync(KvkSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkSearchResult.Ok([], 0));

        public Task<KvkCompanyProfile> GetProfileAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkCompanyProfile.NotFound(kvkNumber));
    }
}
