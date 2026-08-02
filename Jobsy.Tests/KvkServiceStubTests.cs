using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class KvkServiceStubTests
{
    [Fact]
    public async Task GetByKvk_returns_legal_name_for_known_number()
    {
        await using var db = CreateDb();
        var sut = new KvkServiceStub(db);

        var company = await sut.GetByKvkNumberAsync("11223344");

        Assert.NotNull(company);
        Assert.Equal("11223344", company.KvkNumber);
        Assert.Equal("Supermarkt De Fred B.V.", company.Name);
    }

    [Fact]
    public async Task GetByKvk_normalizes_spaces_and_dashes()
    {
        await using var db = CreateDb();
        var sut = new KvkServiceStub(db);

        var company = await sut.GetByKvkNumberAsync("11 22-33 44");

        Assert.NotNull(company);
        Assert.Equal("11223344", company.KvkNumber);
    }

    [Fact]
    public async Task GetByKvk_unknown_or_invalid_returns_null()
    {
        await using var db = CreateDb();
        var sut = new KvkServiceStub(db);

        Assert.Null(await sut.GetByKvkNumberAsync("00000000"));
        Assert.Null(await sut.GetByKvkNumberAsync("123"));
        Assert.Null(await sut.GetByKvkNumberAsync(""));
    }

    [Fact]
    public async Task GetEstablishments_marks_in_use_from_database()
    {
        await using var db = CreateDb();
        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "De Fred Statenkwartier",
            KvkNumber = "11223344",
            KvkEstablishmentId = "11223344_0001",
            Address = "Frederik Hendriklaan 88, Den Haag",
            Location = new GeoPoint(52.0910, 4.2815),
            Type = CompanyType.Employer
        });
        await db.SaveChangesAsync();

        var sut = new KvkServiceStub(db);
        var list = await sut.GetEstablishmentsAsync("11223344");

        Assert.True(list.Count >= 4);
        Assert.Contains(list, e => e.KvkEstablishmentId == "11223344_0001" && e.IsInUse);
        Assert.Contains(list, e => e.KvkEstablishmentId == "11223344_0002" && !e.IsInUse);
    }

    [Fact]
    public async Task Catalog_covers_extended_demo_numbers()
    {
        await using var db = CreateDb();
        var sut = new KvkServiceStub(db);

        foreach (var kvk in new[]
                 {
                     "12345678", "87654321", "11223344", "55667788",
                     "33445566", "44556677", "66778899", "77889900",
                     "88990011", "99001122"
                 })
        {
            Assert.NotNull(await sut.GetByKvkNumberAsync(kvk));
            Assert.NotEmpty(await sut.GetEstablishmentsAsync(kvk));
        }
    }

    [Fact]
    public async Task Intermediary_kvk_exposes_sbi_78()
    {
        await using var db = CreateDb();
        var sut = new KvkServiceStub(db);

        var company = await sut.GetByKvkNumberAsync("55667788");
        Assert.NotNull(company);
        Assert.Contains(company!.EffectiveSbiCodes, c => c.StartsWith("78", StringComparison.Ordinal));

        var establishments = await sut.GetEstablishmentsAsync("55667788");
        Assert.All(establishments, e =>
            Assert.Contains(e.EffectiveSbiCodes, c => c.StartsWith("78", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Employer_kvk_does_not_use_sbi_78()
    {
        await using var db = CreateDb();
        var sut = new KvkServiceStub(db);

        var company = await sut.GetByKvkNumberAsync("12345678");
        Assert.NotNull(company);
        Assert.DoesNotContain(company!.EffectiveSbiCodes, c => c.StartsWith("78", StringComparison.Ordinal));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
