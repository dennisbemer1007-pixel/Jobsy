using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class KbHiddenIntermediaryMaskTests
{
    [Fact]
    public void IsHidden_requires_intermediary_and_masked_flag()
    {
        Assert.False(KbHiddenIntermediaryMask.IsHidden(null, showClientAddressOnMap: false));
        Assert.False(KbHiddenIntermediaryMask.IsHidden(Guid.NewGuid(), showClientAddressOnMap: true));
        Assert.True(KbHiddenIntermediaryMask.IsHidden(Guid.NewGuid(), showClientAddressOnMap: false));
    }

    [Fact]
    public void ResolveBureauLocation_uses_root_parent_when_loaded()
    {
        var root = new Company
        {
            Id = Guid.NewGuid(),
            Name = "FlexPlus HQ",
            Address = "HQ 1",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Intermediary
        };
        var branch = new Company
        {
            Id = Guid.NewGuid(),
            Name = "FlexPlus Naaldwijk",
            Address = "Bureau 9",
            Location = new GeoPoint(51.99, 4.21),
            ParentCompanyId = root.Id,
            ParentCompany = root,
            Type = CompanyType.Intermediary
        };

        var loc = KbHiddenIntermediaryMask.ResolveBureauLocation(branch);
        Assert.NotNull(loc);
        Assert.Equal(52.0, loc!.Latitude);
        Assert.Equal(4.2, loc.Longitude);
    }

    [Fact]
    public void ResolveBureauLocation_returns_null_when_bureau_has_no_coords()
    {
        var bureau = new Company
        {
            Id = Guid.NewGuid(),
            Name = "FlexPlus",
            Address = "Bureau 9",
            Location = new GeoPoint(0, 0),
            Type = CompanyType.Intermediary
        };
        Assert.Null(KbHiddenIntermediaryMask.ResolveBureauLocation(bureau));
    }

    [Fact]
    public void Discovery_record_uses_bureau_pin_and_redacts_client_kvk()
    {
        var client = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Opdrachtgever Secret BV",
            Address = "Klantstraat 1, 2671 AB Naaldwijk",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_000012345678",
            Location = new GeoPoint(52.1, 4.3),
            LogoUrl = "/client-logo.png"
        };
        var bureau = new Company
        {
            Id = Guid.NewGuid(),
            Name = "FlexPlus",
            Address = "Bureauweg 9, Naaldwijk",
            Location = new GeoPoint(52.0, 4.2),
            LogoUrl = "/bureau-logo.png",
            Type = CompanyType.Intermediary
        };
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = client.Id,
            Company = client,
            IntermediaryCompanyId = bureau.Id,
            IntermediaryCompany = bureau,
            ShowClientAddressOnMap = false,
            Location = new GeoPoint(51.99, 4.25),
            Title = "Orderpicker",
            Description = "d",
            Status = VacancyStatus.Active
        };

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);

        Assert.Equal("FlexPlus", record.CompanyName);
        Assert.Equal("Bureauweg 9, Naaldwijk", record.CompanyAddress);
        Assert.Equal(52.0, record.Latitude);
        Assert.Equal(4.2, record.Longitude);
        Assert.Null(record.OfferedByLabel);
        Assert.Null(record.KvkNumber);
        Assert.Null(record.Vestigingsnummer);
        Assert.DoesNotContain("Opdrachtgever", record.CompanyName, StringComparison.Ordinal);
        Assert.DoesNotContain("Klantstraat", record.CompanyAddress, StringComparison.Ordinal);
        Assert.InRange(Math.Abs(record.Latitude - 51.99), 0.01, 90); // not workplace ±0.001
        Assert.True(Math.Abs(record.Latitude - 51.99) > 0.001);
        Assert.True(Math.Abs(record.Longitude - 4.25) > 0.001);
    }

    [Fact]
    public void Discovery_record_omits_pin_when_bureau_location_missing()
    {
        var client = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Opdrachtgever Secret BV",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52.1, 4.3)
        };
        var bureau = new Company
        {
            Id = Guid.NewGuid(),
            Name = "FlexPlus",
            Address = "Bureauweg 9",
            Location = new GeoPoint(0, 0),
            Type = CompanyType.Intermediary
        };
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = client.Id,
            Company = client,
            IntermediaryCompanyId = bureau.Id,
            IntermediaryCompany = bureau,
            ShowClientAddressOnMap = false,
            Location = new GeoPoint(51.99, 4.25),
            Title = "t",
            Description = "d",
            Status = VacancyStatus.Active
        };

        var record = VacancyDiscoveryIndex.ToRecord(vacancy);
        Assert.Equal(0, record.Latitude);
        Assert.Equal(0, record.Longitude);
        Assert.Equal("FlexPlus", record.CompanyName);
    }
}
