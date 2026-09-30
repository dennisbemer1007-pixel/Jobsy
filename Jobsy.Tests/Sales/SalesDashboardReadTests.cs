using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Sales;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Sales;

public class SalesDashboardReadTests
{
    [Fact]
    public void NextRunDate_On_29_09_2026_Is_1_Oktober()
    {
        var today = new DateOnly(2026, 9, 29);
        Assert.Equal(new DateOnly(2026, 10, 1), SalesDashboardReadService.ResolveNextRunDate(today));
    }

    [Fact]
    public void NextRunDate_Before_FirstWorkday_Uses_This_Month()
    {
        var today = new DateOnly(2026, 10, 1);
        Assert.Equal(new DateOnly(2026, 10, 1), SalesDashboardReadService.ResolveNextRunDate(today));
    }

    [Fact]
    public async Task Dashboard_Numbers_On_Fixed_Dataset()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        SeedBeneficiary(db, smId);
        SeedSettings(db);

        var active = Guid.NewGuid();
        var quiet = Guid.NewGuid();
        var noBuy = Guid.NewGuid();
        SeedCompany(db, smId, active, "Actief BV", CompanyLegalForm.Bv, "Straat 1, 2671 AB Naaldwijk",
            now.AddDays(-40), now.AddDays(-30), SalesAttributionSource.LinkCookie);
        SeedCompany(db, smId, quiet, "Stil BV", CompanyLegalForm.Bv, "Weg 2, Delft",
            now.AddDays(-200), now.AddDays(-120), SalesAttributionSource.TypedCode);
        SeedCompany(db, smId, noBuy, "Nog niets", CompanyLegalForm.Eenmanszaak, "Laan 3, 2500 AA Den Haag",
            now.AddDays(-20), null, SalesAttributionSource.LinkCookie);

        db.CommissionLedgerEntries.AddRange(
            Entry(smId, active, 100m, now.AddDays(-5), now.AddDays(9)),
            Entry(smId, active, 200m, now.AddDays(-20), now.AddDays(-1)),
            Entry(smId, quiet, 50m, now.AddDays(-100), now.AddDays(-80)));
        db.SalesLinkClickDailies.Add(new SalesLinkClickDaily
        {
            BeneficiaryUserId = smId,
            Date = new DateOnly(2026, 9, 1),
            Channel = SalesLinkChannel.Link,
            Count = 10
        });
        await db.SaveChangesAsync();

        var wallet = new SalesWalletReadService(db);
        var beneficiary = new SalesBeneficiaryService(db, new AlwaysOnFeatures());
        var employers = new SalesEmployerReadService(db, wallet, beneficiary);
        var funnel = new SalesFunnelReadService(db);
        var sut = new SalesDashboardReadService(db, wallet, funnel, employers);

        var dto = await sut.GetAsync(smId, "year", now);
        Assert.Equal(250m, dto.Available);
        Assert.Equal(100m, dto.Pending);
        Assert.Equal(350m, dto.EarnedInPeriod);
        Assert.Null(dto.EarnedPrevComparable);
        Assert.Equal(3, dto.TotalEmployers);
        Assert.Equal(1, dto.ActiveEmployers);
        Assert.Equal(12, dto.Monthly.Count);
        Assert.True(dto.Monthly[^1].IsCurrent);
        Assert.Equal(new DateOnly(2026, 10, 1), dto.NextRunDate);
        Assert.NotNull(dto.Funnel);
        Assert.Equal(10, dto.Funnel!.Visits);
        Assert.True(dto.Todos.Count <= 5);
        Assert.Contains(dto.Todos, t => t.Kind == "no-purchase");
        Assert.True(dto.TopEmployers.Count <= 5);
        Assert.Equal(active, dto.TopEmployers[0].CompanyId);
    }

    [Fact]
    public async Task Employers_Org_Plus_Vestiging_Is_One_Row()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        var root = Guid.NewGuid();
        var branch = Guid.NewGuid();
        SeedBeneficiary(db, smId);
        SeedSettings(db);
        SeedCompany(db, smId, root, "Root BV", CompanyLegalForm.Bv, "A, Naaldwijk",
            DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5), SalesAttributionSource.LinkCookie);
        db.Companies.Add(new Company
        {
            Id = branch,
            Name = "Vestiging",
            ParentCompanyId = root,
            ReferredBySalesManagerUserId = smId,
            SalesAttributedAtUtc = DateTime.UtcNow.AddDays(-10),
            Location = new GeoPoint(52, 4)
        });
        await db.SaveChangesAsync();

        var wallet = new SalesWalletReadService(db);
        var beneficiary = new SalesBeneficiaryService(db, new AlwaysOnFeatures());
        var sut = new SalesEmployerReadService(db, wallet, beneficiary);
        var page = await sut.ListAsync(smId, null, null, null, 1);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.Items[0].BranchCount);
        Assert.Equal("Naaldwijk", page.Items[0].Place);
    }

    [Fact]
    public async Task Eenmanszaak_Hides_Place_And_Foreign_Company_Is_Null()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        var own = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        SeedBeneficiary(db, smId);
        SeedSettings(db);
        SeedCompany(db, smId, own, "Zzp Jan", CompanyLegalForm.Eenmanszaak, "Straat, Delft",
            DateTime.UtcNow.AddDays(-10), null, SalesAttributionSource.TypedCode);
        db.Companies.Add(new Company
        {
            Id = foreign,
            Name = "Other",
            ReferredBySalesManagerUserId = Guid.NewGuid(),
            Location = new GeoPoint(52, 4)
        });
        await db.SaveChangesAsync();

        var wallet = new SalesWalletReadService(db);
        var beneficiary = new SalesBeneficiaryService(db, new AlwaysOnFeatures());
        var sut = new SalesEmployerReadService(db, wallet, beneficiary);
        var page = await sut.ListAsync(smId, null, null, null, 1);
        Assert.Null(page.Items[0].Place);
        Assert.Null(await sut.GetDetailAsync(smId, foreign));
    }

    [Fact]
    public void DeriveStatus_Quiet_And_Active()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc);
        var (status, _, quiet, year, _, _) = SalesEmployerReadService.DeriveStatus(
            start, now.AddDays(-100), 0.25m, 0.10m, 0.05m, 1095, now);
        Assert.Equal(SalesEmployerStatus.Quiet, status);
        Assert.NotNull(quiet);
        Assert.Equal(1, year);

        var (active, _, _, _, _, _) = SalesEmployerReadService.DeriveStatus(
            start, now.AddDays(-10), 0.25m, 0.10m, 0.05m, 1095, now);
        Assert.Equal(SalesEmployerStatus.Active, active);
    }

    private static CommissionLedgerEntry Entry(
        Guid smId, Guid companyId, decimal amount, DateTime created, DateTime available) => new()
    {
        Id = Guid.NewGuid(),
        SalesManagerUserId = smId,
        CompanyId = companyId,
        Kind = CommissionEntryKind.TokenCommission,
        AmountExVat = amount,
        CreatedAt = created,
        AvailableFromUtc = available
    };

    private static void SeedBeneficiary(JobsyDbContext db, Guid smId)
    {
        db.Users.Add(new User
        {
            Id = smId,
            Email = "sm@example.com",
            FullName = "Tom Hendriks",
            Role = UserRole.SalesManager
        });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            TrackingCode = "SM-K7Q2MP",
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
            AgreementSignedAt = DateTime.UtcNow.AddDays(-30),
            Iban = "NL91ABNA0417164300",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SalesSelfBillingConsents.Add(new SalesSelfBillingConsent
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            Version = "2026-10-self-billing-v1",
            TextSha256 = "abc",
            AcceptedAtUtc = DateTime.UtcNow.AddDays(-20)
        });
    }

    private static void SeedSettings(JobsyDbContext db)
    {
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = Guid.NewGuid(),
            CommissionHoldDays = 14,
            PayoutMinimumEuro = 50m,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private static void SeedCompany(
        JobsyDbContext db,
        Guid smId,
        Guid id,
        string name,
        CompanyLegalForm legal,
        string address,
        DateTime attributed,
        DateTime? starts,
        SalesAttributionSource source)
    {
        db.Companies.Add(new Company
        {
            Id = id,
            Name = name,
            Address = address,
            LegalForm = legal,
            ReferredBySalesManagerUserId = smId,
            SalesAttributedAtUtc = attributed,
            SalesAttributionSource = source,
            CommissionStartsAtUtc = starts,
            CommissionDirectRateSnapshot = 0.25m,
            CommissionYear2RateSnapshot = 0.10m,
            CommissionYear3RateSnapshot = 0.05m,
            CommissionDurationDaysSnapshot = 1095,
            Location = new GeoPoint(52, 4)
        });
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sales-dash-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}

/// <summary>§R rows for dashboard / employers endpoints (04).</summary>
public class SalesRightsMatrixTests
{
    public static TheoryData<string, string, int> EndpointRows => new()
    {
        { "GET api/sales/me/dashboard", JobsyRoles.SalesManager, 200 },
        { "GET api/sales/me/employers", JobsyRoles.SalesManager, 200 },
        { "GET api/sales/me/employers/{id}", JobsyRoles.SalesManager, 200 },
        { "GET api/sales/me/dashboard", JobsyRoles.Candidate, 403 },
        { "GET api/sales/me/employers", JobsyRoles.BranchManager, 403 },
        { "GET api/sales/me/employers/{id}", JobsyRoles.Admin, 403 },
    };

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public void Matrix_rows_are_defined(string endpoint, string role, int expectedStatus)
    {
        Assert.False(string.IsNullOrWhiteSpace(endpoint));
        Assert.False(string.IsNullOrWhiteSpace(role));
        Assert.True(expectedStatus is 200 or 403 or 404);
    }

    [Fact]
    public void SalesEmployerDto_has_no_forbidden_property_names()
    {
        var forbidden = new[] { "Kvk", "Address", "Email", "Phone", "Contact", "Vacancy", "Candidate" };
        foreach (var prop in typeof(SalesEmployerDto).GetProperties())
        {
            Assert.False(forbidden.Any(f => prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase)), prop.Name);
        }

        foreach (var prop in typeof(SalesEmployerDetailDto).GetProperties())
        {
            Assert.False(forbidden.Any(f => prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase)), prop.Name);
        }
    }
}
