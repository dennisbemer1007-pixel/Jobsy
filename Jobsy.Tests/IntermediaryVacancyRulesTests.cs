using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class IntermediaryVacancyRulesTests
{
    [Fact]
    public void ValidateEndClientKvk_Requires_Kvk_And_Establishment_For_Intermediary()
    {
        Assert.NotNull(IntermediaryVacancyRules.ValidateEndClientKvk(null, true));
        Assert.NotNull(IntermediaryVacancyRules.ValidateEndClientKvk(
            new Company { KvkNumber = "123", Location = new GeoPoint(0, 0) }, true));
        Assert.Null(IntermediaryVacancyRules.ValidateEndClientKvk(
            new Company
            {
                KvkNumber = "12345678",
                KvkEstablishmentId = "12345678_0001",
                Location = new GeoPoint(0, 0)
            },
            true));
        Assert.Null(IntermediaryVacancyRules.ValidateEndClientKvk(null, false));
    }

    [Fact]
    public void ResolvePublicDisplay_Masks_EndClient_By_Default()
    {
        var client = new Company
        {
            Name = "Eindklant BV",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52.0, 4.2)
        };
        var agency = new Company
        {
            Name = "Flex Bureau",
            Address = "Agency 9",
            Location = new GeoPoint(52.1, 4.3)
        };
        var vacancy = new Vacancy
        {
            Company = client,
            IntermediaryCompany = agency,
            ShowClientAddressOnMap = false,
            Location = client.Location
        };

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, agency);
        Assert.Equal("Flex Bureau", display.DisplayName);
        Assert.Equal("Agency 9", display.DisplayAddress);
        Assert.Equal(52.1, display.Latitude);
        Assert.Equal("Aangeboden door Flex Bureau", display.OfferedByLabel);
    }

    [Fact]
    public void ResolvePublicDisplay_OpenMap_Shows_Client()
    {
        var client = new Company
        {
            Name = "Eindklant BV",
            Address = "Klantstraat 1",
            Location = new GeoPoint(52.0, 4.2)
        };
        var agency = new Company
        {
            Name = "Flex Bureau",
            Address = "Agency 9",
            Location = new GeoPoint(52.1, 4.3)
        };
        var vacancy = new Vacancy
        {
            Company = client,
            IntermediaryCompany = agency,
            ShowClientAddressOnMap = true,
            Location = client.Location
        };

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, agency);
        Assert.Equal("Eindklant BV", display.DisplayName);
        Assert.Equal("Klantstraat 1", display.DisplayAddress);
        Assert.Equal(52.0, display.Latitude);
        Assert.Equal("Aangeboden door Flex Bureau", display.OfferedByLabel);
    }
}
