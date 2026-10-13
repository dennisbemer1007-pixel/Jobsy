using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CompanyWorkTypesTests
{
    [Fact]
    public void MaxPerCompany_is_4_and_rejects_unknown_or_too_many()
    {
        Assert.Equal(4, WorkTypeLabels.MaxPerCompany);
        Assert.True(WorkTypeLabels.IsValidCompanySelection([WorkTypeLabels.Zorg, WorkTypeLabels.Schoonmaak]));
        Assert.False(WorkTypeLabels.IsValidCompanySelection([
            WorkTypeLabels.Zorg, WorkTypeLabels.Schoonmaak, WorkTypeLabels.Horeca,
            WorkTypeLabels.Winkel, WorkTypeLabels.Bouw
        ]));
        Assert.False(WorkTypeLabels.IsValidCompanySelection(["Onbekend"]));
    }

    [Fact]
    public void CombineStoredForCompany_keeps_up_to_four_labels()
    {
        var stored = WorkTypeLabels.CombineStoredForCompany([
            WorkTypeLabels.Zorg, WorkTypeLabels.Schoonmaak, WorkTypeLabels.Horeca,
            WorkTypeLabels.Winkel, WorkTypeLabels.Bouw
        ]);
        var labels = WorkTypeLabels.SplitStored(stored);
        Assert.Equal(4, labels.Length);
        Assert.Equal(WorkTypeLabels.Zorg, labels[0]);
    }

    [Fact]
    public void Sbi_prefill_maps_known_codes_to_branches()
    {
        var labels = SbiWorkTypeMap.Map(["88101", "81210"]);
        Assert.Equal([WorkTypeLabels.Zorg, WorkTypeLabels.Schoonmaak], labels);
    }

    [Fact]
    public async Task Activation_prefills_root_work_types_from_sbi_when_empty()
    {
        await using var db = CreateDb();
        var org = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Org",
            KvkNumber = "90123456",
            Address = "Straat 1",
            Location = new GeoPoint(52.1, 5.1),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        Assert.Null(org.WorkTypeLabels);
        org.WorkTypeLabels = WorkTypeLabels.CombineStoredForCompany(SbiWorkTypeMap.Map(["88101", "81210"]));
        db.Companies.Add(org);
        await db.SaveChangesAsync();

        var loaded = await db.Companies.AsNoTracking().SingleAsync(c => c.Id == org.Id);
        Assert.Equal("Zorg, Schoonmaak", loaded.WorkTypeLabels);
    }

    [Fact]
    public void New_vacancy_defaults_to_company_first_branche_existing_untouched()
    {
        var companyLabels = WorkTypeLabels.NormalizeCompanyLabels(["Zorg", "Schoonmaak"]);
        var first = companyLabels.First();
        Assert.Equal(WorkTypeLabels.Zorg, first);

        // Existing vacancy keeps its own labels.
        var existing = WorkTypeLabels.CombineStored(["Horeca"]);
        Assert.Equal("Horeca", existing);
        Assert.NotEqual(first, WorkTypeLabels.SplitStored(existing).First());
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("company-worktypes-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }
}
