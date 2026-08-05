using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class IntermediaryVacancyRulesTests
{
    [Fact]
    public void ValidateEndClientKvk_skips_for_non_intermediary()
    {
        Assert.Null(IntermediaryVacancyRules.ValidateEndClientKvk(null, callerIsIntermediary: false));
    }

    [Fact]
    public void ValidateEndClientKvk_requires_kvk_and_establishment()
    {
        var missing = new Company { Id = Guid.NewGuid(), Name = "X", Address = "a", Location = new GeoPoint(1, 2) };
        Assert.Contains("KVK", IntermediaryVacancyRules.ValidateEndClientKvk(missing, true)!, StringComparison.OrdinalIgnoreCase);

        missing.KvkNumber = "12345678";
        Assert.Contains("vestiging", IntermediaryVacancyRules.ValidateEndClientKvk(missing, true)!, StringComparison.OrdinalIgnoreCase);

        missing.KvkEstablishmentId = "000012345678";
        Assert.Null(IntermediaryVacancyRules.ValidateEndClientKvk(missing, true));
    }

    [Fact]
    public void ResolvePublicDisplay_masks_end_client_by_default()
    {
        var client = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Opdrachtgever BV",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52.1, 4.3)
        };
        var intermediary = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Uitzendbureau",
            Address = "Bureauweg 9",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Intermediary
        };
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = client.Id,
            Company = client,
            IntermediaryCompanyId = intermediary.Id,
            IntermediaryCompany = intermediary,
            ShowClientAddressOnMap = false,
            Location = client.Location,
            Title = "t",
            Description = "d"
        };

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, intermediary);
        Assert.Equal("Uitzendbureau", display.DisplayName);
        Assert.Equal("Bureauweg 9", display.DisplayAddress);
        // Pin stays on the vacancy workplace even when the name/address are masked.
        Assert.Equal(52.1, display.Latitude);
        Assert.Equal(4.3, display.Longitude);
        Assert.Equal("Aangeboden door Uitzendbureau", display.OfferedByLabel);
    }

    [Fact]
    public void ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq()
    {
        var client = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Opdrachtgever BV",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52.1, 4.3)
        };
        var intermediary = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Uitzendbureau",
            Address = "Bureauweg 9",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Intermediary
        };
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = client.Id,
            Company = client,
            IntermediaryCompanyId = intermediary.Id,
            IntermediaryCompany = intermediary,
            ShowClientAddressOnMap = false,
            Location = new GeoPoint(51.99, 4.25),
            Title = "t",
            Description = "d"
        };

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, intermediary);
        Assert.Equal("Uitzendbureau", display.DisplayName);
        Assert.Equal(51.99, display.Latitude);
        Assert.Equal(4.25, display.Longitude);
    }

    [Fact]
    public void ResolvePublicDisplay_uses_client_when_flag_open()
    {
        var client = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Opdrachtgever BV",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52.1, 4.3)
        };
        var intermediary = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Uitzendbureau",
            Address = "Bureauweg 9",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Intermediary
        };
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = client.Id,
            Company = client,
            IntermediaryCompanyId = intermediary.Id,
            IntermediaryCompany = intermediary,
            ShowClientAddressOnMap = true,
            Location = client.Location,
            Title = "t",
            Description = "d"
        };

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, intermediary);
        Assert.Equal("Opdrachtgever BV", display.DisplayName);
        Assert.Equal("Klantstraat 1", display.DisplayAddress);
        Assert.Equal(52.1, display.Latitude);
        Assert.Equal("Aangeboden door Uitzendbureau", display.OfferedByLabel);
    }
}
