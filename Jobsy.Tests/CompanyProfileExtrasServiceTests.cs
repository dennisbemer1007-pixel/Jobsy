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

public class CompanyProfileExtrasServiceTests
{
    [Fact]
    public async Task Save_rejects_more_than_four_branches_and_bad_sliders()
    {
        await using var db = CreateDb();
        var company = Seed(db);
        await db.SaveChangesAsync();
        var sut = new CompanyProfileExtrasService(db, new MemoryCache(new MemoryCacheOptions()), new StubKvk());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SaveAsync(
            company.Id,
            new CompanyProfileExtrasUpdate(WorkTypeLabels:
            [
                WorkTypeLabels.Zorg, WorkTypeLabels.Schoonmaak, WorkTypeLabels.Horeca,
                WorkTypeLabels.Winkel, WorkTypeLabels.Bouw
            ])));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SaveAsync(
            company.Id,
            new CompanyProfileExtrasUpdate(CultureSliders: new Dictionary<string, int>
            {
                [CulturePersonalityCatalog.Autonomy] = 9
            })));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SaveAsync(
            company.Id,
            new CompanyProfileExtrasUpdate(ValueCardIds: ["zorg", "groei"])));
    }

    [Fact]
    public async Task Save_persists_branches_sliders_and_exactly_three_cards_on_root()
    {
        await using var db = CreateDb();
        var org = Seed(db, "Org");
        var child = Seed(db, "Child", parentId: org.Id);
        await db.SaveChangesAsync();
        var sut = new CompanyProfileExtrasService(db, new MemoryCache(new MemoryCacheOptions()), new StubKvk());

        var dto = await sut.SaveAsync(child.Id, new CompanyProfileExtrasUpdate(
            WorkTypeLabels: [WorkTypeLabels.Zorg, WorkTypeLabels.Schoonmaak],
            CultureSliders: CulturePersonalityCatalog.CultureDimensionCodes
                .ToDictionary(c => c, _ => 4, StringComparer.OrdinalIgnoreCase),
            ValueCardIds: ["zorg", "vakmanschap", "betekenis"]));

        Assert.Equal(org.Id, dto.RootCompanyId);
        Assert.Equal(2, dto.WorkTypeLabels.Count);
        Assert.Equal(CompanyCultureSources.Quick, dto.CultureSource);
        Assert.Equal(3, dto.ValueCardIds.Count);

        var orgRow = await db.Companies.AsNoTracking().SingleAsync(c => c.Id == org.Id);
        Assert.Contains("Zorg", orgRow.WorkTypeLabels);
        Assert.Null((await db.Companies.AsNoTracking().SingleAsync(c => c.Id == child.Id)).WorkTypeLabels);
    }

    private static Company Seed(JobsyDbContext db, string name = "Co", Guid? parentId = null)
    {
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = "90123456",
            Address = "A",
            Location = new GeoPoint(52, 5),
            Type = CompanyType.Employer,
            ParentCompanyId = parentId,
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(c);
        return c;
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("profile-extras-" + Guid.NewGuid())
            .Options);

    private sealed class StubKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(null);
        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<KvkEstablishmentResult>>([]);
        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkEstablishmentsLookup.Ok([]));
        public Task<KvkSearchResult> SearchAsync(KvkSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkSearchResult.Ok([], 0));
        public Task<KvkCompanyProfile> GetProfileAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkCompanyProfile.NotFound(kvkNumber));
    }
}
