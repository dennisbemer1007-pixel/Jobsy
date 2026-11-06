using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class FirstEmployerAcceptanceFreeTests
{
    [Fact]
    public void First_accept_for_billing_company_is_free_when_flag_on()
    {
        var settings = new FlexCommercialSettingsDto(
            2m,
            "Yellowstone",
            2.99m,
            2.99m,
            2.99m,
            2.99m,
            4000m,
            1m,
            0.5m,
            DateOnly.MaxValue,
            1m,
            FirstEmployerAcceptanceFreeEnabled: true,
            UpdatedAtUtc: DateTime.UtcNow);

        var cost = AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            VacancyKind.Regular,
            settings,
            DateOnly.FromDateTime(DateTime.UtcNow),
            billingCompanyHasPriorPlacements: false);
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void Second_accept_uses_pilot_pricing_when_flag_on()
    {
        var settings = new FlexCommercialSettingsDto(
            2m,
            "Yellowstone",
            2.99m,
            2.99m,
            2.99m,
            2.99m,
            4000m,
            1m,
            0.5m,
            DateOnly.MaxValue,
            1m,
            FirstEmployerAcceptanceFreeEnabled: true,
            UpdatedAtUtc: DateTime.UtcNow);

        var cost = AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            VacancyKind.Regular,
            settings,
            DateOnly.FromDateTime(DateTime.UtcNow),
            billingCompanyHasPriorPlacements: true);
        Assert.Equal(0.5m, cost);
    }

    [Fact]
    public void Volunteer_kind_stays_free_regardless()
    {
        var settings = new FlexCommercialSettingsDto(
            2m,
            "Yellowstone",
            2.99m,
            2.99m,
            2.99m,
            2.99m,
            4000m,
            1m,
            0.5m,
            DateOnly.MaxValue,
            1m,
            FirstEmployerAcceptanceFreeEnabled: false,
            UpdatedAtUtc: DateTime.UtcNow);

        Assert.Equal(0m, AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            VacancyKind.Volunteer,
            settings,
            DateOnly.FromDateTime(DateTime.UtcNow),
            billingCompanyHasPriorPlacements: false));
    }
}
