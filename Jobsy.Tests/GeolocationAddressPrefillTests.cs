using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class GeolocationAddressPrefillTests
{
    [Theory]
    [InlineData(25, GeolocationPrefillMode.Precise, "Westmadeweg 120, 2553 EM Den Haag", "Westmadeweg 120, 2553 EM Den Haag")]
    [InlineData(50, GeolocationPrefillMode.Precise, "Westmadeweg 120, 2553 EM Den Haag", "Westmadeweg 120, 2553 EM Den Haag")]
    [InlineData(51, GeolocationPrefillMode.Approximate, "Westmadeweg 120, 2553 EM Den Haag", "Westmadeweg, 2553 EM Den Haag")]
    [InlineData(800, GeolocationPrefillMode.Approximate, "Dirk van den Walelaan 12, 2553 EH Den Haag", "Dirk van den Walelaan, 2553 EH Den Haag")]
    public void FromReverseGeocodeLabel_branches_on_accuracy(
        double accuracyMeters,
        GeolocationPrefillMode expectedMode,
        string reverseLabel,
        string expectedQuery)
    {
        var result = GeolocationAddressPrefill.FromReverseGeocodeLabel(reverseLabel, accuracyMeters);
        Assert.NotNull(result);
        Assert.Equal(expectedMode, result!.Mode);
        Assert.Equal(expectedQuery, result.AddressQuery);
        Assert.Equal(expectedMode == GeolocationPrefillMode.Precise, result.ApplyCoordinates);
    }

    [Fact]
    public void HasResolvableLocation_blocks_pending_geolocation_confirm_even_with_coords()
    {
        Assert.False(OnboardingWizardValidation.HasResolvableLocation(
            "Den Haag",
            homeLat: 52.07,
            homeLng: 4.3,
            postcode: "2553 EM",
            postcodeResolved: true,
            geolocationPendingConfirm: true));
    }

    [Fact]
    public void StripHouseNumberFromLabel_removes_suffix_before_comma()
    {
        Assert.Equal(
            "Westmadeweg, 2553 EM Den Haag",
            GeolocationAddressPrefill.StripHouseNumberFromLabel("Westmadeweg 120, 2553 EM Den Haag"));
    }
}
