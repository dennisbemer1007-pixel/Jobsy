using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class PublicVisibilityTests
{
    [Theory]
    [InlineData(CompanyVerificationStatus.Unverified, false)]
    [InlineData(CompanyVerificationStatus.Pending, false)]
    [InlineData(CompanyVerificationStatus.Rejected, false)]
    [InlineData(CompanyVerificationStatus.Verified, true)]
    public void Company_public_only_when_verified(CompanyVerificationStatus status, bool expected)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            VerificationStatus = status
        };

        Assert.Equal(expected, PublicVisibility.IsCompanyPublic(company));
    }

    [Fact]
    public void Vacancy_requires_company_and_intermediary_verified()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var verified = VerifiedCompany("A");
        var unverified = VerifiedCompany("B");
        unverified.VerificationStatus = CompanyVerificationStatus.Unverified;

        var live = CreateVacancy(verified, today);
        Assert.True(PublicVisibility.IsVacancyPublic(live, today));

        live.Company = unverified;
        Assert.False(PublicVisibility.IsVacancyPublic(live, today));

        var withIntermediary = CreateVacancy(verified, today);
        withIntermediary.IntermediaryCompanyId = unverified.Id;
        withIntermediary.IntermediaryCompany = unverified;
        Assert.False(PublicVisibility.IsVacancyPublic(withIntermediary, today));

        withIntermediary.IntermediaryCompany = VerifiedCompany("Inter");
        withIntermediary.IntermediaryCompanyId = withIntermediary.IntermediaryCompany.Id;
        Assert.True(PublicVisibility.IsVacancyPublic(withIntermediary, today));
    }

    [Fact]
    public void Existing_date_status_rules_still_apply()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var company = VerifiedCompany("Live");
        var draft = CreateVacancy(company, today);
        draft.Status = VacancyStatus.Draft;

        Assert.False(PublicVisibility.IsVacancyPublic(draft, today));
        Assert.True(VacancyVisibilityRules.IsDateAndStatusPublic(
            VacancyStatus.Active, today, today.AddDays(7), today));
    }

    private static Company VerifiedCompany(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        KvkNumber = "12345678",
        Address = "Straat 1",
        Location = new GeoPoint(52, 4),
        VerificationStatus = CompanyVerificationStatus.Verified,
        VerificationMethod = CompanyVerificationMethod.AdminCreated,
        VerifiedAtUtc = DateTime.UtcNow
    };

    private static Vacancy CreateVacancy(Company company, DateOnly today) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Vacature",
        Description = "x",
        HourlyWage = 14m,
        StartDate = today,
        EndDate = today.AddMonths(1),
        Status = VacancyStatus.Active,
        CompanyId = company.Id,
        Company = company,
        Location = new GeoPoint(52.0, 4.3),
        RequiredTransport = TransportMode.Bike,
        MaxApplications = 5
    };
}
