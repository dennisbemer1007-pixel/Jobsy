using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CandidateApplicationLocationTests
{
    [Theory]
    [InlineData("Voorstraat 1, 2671 AB Naaldwijk", "Naaldwijk")]
    [InlineData("Delft", "Delft")]
    [InlineData("Den Haag", "Den Haag")]
    public void ToCityOrNull_keeps_city_labels(string address, string expected)
        => Assert.Equal(expected, CandidateApplicationLocation.ToCityOrNull(address));

    [Theory]
    [InlineData("Klantstraat 1")]
    [InlineData("Bureauweg 9")]
    [InlineData("")]
    [InlineData(null)]
    public void ToCityOrNull_drops_street_addresses_and_empty(string? address)
        => Assert.Null(CandidateApplicationLocation.ToCityOrNull(address));

    [Fact]
    public void ForPublicCard_masks_end_client_address_when_intermediary_is_hidden()
    {
        var (name, location) = CandidateApplicationLocation.ForPublicCard(
            hasIntermediary: true,
            showClientAddressOnMap: false,
            endClientName: "Opdrachtgever BV",
            endClientAddress: "Klantstraat 1, 2671 AB Naaldwijk",
            intermediaryName: "Uitzendbureau",
            intermediaryAddress: "Bureauweg 9, 2611 AB Delft");

        Assert.Equal("Uitzendbureau", name);
        Assert.Equal("Delft", location);
    }

    [Fact]
    public void ForPublicCard_uses_end_client_city_when_map_is_open()
    {
        var (name, location) = CandidateApplicationLocation.ForPublicCard(
            hasIntermediary: true,
            showClientAddressOnMap: true,
            endClientName: "Opdrachtgever BV",
            endClientAddress: "Klantstraat 1, 2671 AB Naaldwijk",
            intermediaryName: "Uitzendbureau",
            intermediaryAddress: "Bureauweg 9, 2611 AB Delft");

        Assert.Equal("Opdrachtgever BV", name);
        Assert.Equal("Naaldwijk", location);
    }

    [Fact]
    public void ForPublicCard_omits_location_when_only_a_street_is_known()
    {
        var (_, location) = CandidateApplicationLocation.ForPublicCard(
            hasIntermediary: false,
            showClientAddressOnMap: false,
            endClientName: "Demo BV",
            endClientAddress: "Industrieweg 1",
            intermediaryName: null,
            intermediaryAddress: null);

        Assert.Null(location);
    }
}
