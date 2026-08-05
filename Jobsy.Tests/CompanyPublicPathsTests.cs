using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CompanyPublicPathsTests
{
    [Theory]
    [InlineData("12345678", "12345678_0001", "/12345678/0001")]
    [InlineData("1234-5678", "12345678_0012", "/12345678/0012")]
    [InlineData(null, "12345678_0001", null)]
    [InlineData("12345678", null, null)]
    [InlineData("123", "123_1", null)]
    public void TryBuildPath_builds_stable_public_urls(string? kvk, string? establishmentId, string? expected)
        => Assert.Equal(expected, CompanyPublicPaths.TryBuildPath(kvk, establishmentId));

    [Fact]
    public void TryBuildKvkPath_requires_eight_digits()
    {
        Assert.Equal("/12345678", CompanyPublicPaths.TryBuildKvkPath("12345678"));
        Assert.Null(CompanyPublicPaths.TryBuildKvkPath("abc"));
    }

    [Theory]
    [InlineData("12345678_0001", "12345678", "0001")]
    [InlineData("0007", null, "0007")]
    [InlineData("", null, null)]
    public void TryParseVestigingsnummer_reads_suffix(string? establishmentId, string? kvk, string? expected)
        => Assert.Equal(expected, CompanyPublicPaths.TryParseVestigingsnummer(establishmentId, kvk));
}

public class PublicCompanyPageLookupTests
{
    [Fact]
    public async Task Resolves_vestiging_by_kvk_and_establishment()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo Filiaal",
            Address = "Straat 1",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0001",
            Location = new GeoPoint(52.0, 4.3),
            Type = CompanyType.Employer
        });
        await db.SaveChangesAsync();

        var match = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.KvkEstablishmentId == CompanyPublicPaths.BuildEstablishmentId("12345678", "0001"));

        Assert.NotNull(match);
        Assert.Equal(companyId, match!.Id);
        Assert.Equal("/12345678/0001", CompanyPublicPaths.TryBuildPath(match.KvkNumber, match.KvkEstablishmentId));
    }

    [Fact]
    public async Task Resolves_all_companies_under_kvk()
    {
        await using var db = CreateDb();
        db.Companies.AddRange(
            new Company
            {
                Id = Guid.NewGuid(),
                Name = "A",
                Address = "A",
                KvkNumber = "87654321",
                KvkEstablishmentId = "87654321_0001",
                Location = new GeoPoint(52.0, 4.3),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = Guid.NewGuid(),
                Name = "B",
                Address = "B",
                KvkNumber = "87654321",
                KvkEstablishmentId = "87654321_0002",
                Location = new GeoPoint(52.1, 4.4),
                Type = CompanyType.Employer
            });
        await db.SaveChangesAsync();

        var ids = await db.Companies.AsNoTracking()
            .Where(c => c.KvkNumber == "87654321")
            .Select(c => c.Id)
            .ToListAsync();

        Assert.Equal(2, ids.Count);
        Assert.Equal("/87654321", CompanyPublicPaths.TryBuildKvkPath("87654321"));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
