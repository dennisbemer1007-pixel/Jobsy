using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CandidateHomeAddressLookupTests
{
    [Fact]
    public void HydrateDraftFromStored_sets_address_query_and_postcode()
    {
        var draft = new OnboardingProfileDraft();
        CandidateHomeAddressLookup.HydrateDraftFromStored(draft, "2671 GB Naaldwijk");
        Assert.Equal("2671 GB Naaldwijk", draft.AddressQuery);
        Assert.Equal("2671 GB", draft.Postcode);
        Assert.Equal("Naaldwijk", draft.City);
    }

    [Fact]
    public void BuildHomeAddressForSave_prefers_resolved_query()
    {
        var draft = new OnboardingProfileDraft
        {
            AddressQuery = "Herenstraat 20, 2671 AA Naaldwijk",
            HomeLat = 51.99,
            HomeLng = 4.21,
            Postcode = "2671 AA",
            City = "Naaldwijk"
        };
        Assert.Equal(
            "Herenstraat 20, 2671 AA Naaldwijk",
            CandidateHomeAddressLookup.BuildHomeAddressForSave(draft));
    }

    [Fact]
    public void BuildHomeAddressForSave_falls_back_to_postcode_and_city()
    {
        var draft = new OnboardingProfileDraft
        {
            Postcode = "2671GB",
            City = "Naaldwijk",
            PostcodeResolved = true,
            HomeLat = 51.99,
            HomeLng = 4.21
        };
        Assert.Equal("2671 GB Naaldwijk", CandidateHomeAddressLookup.BuildHomeAddressForSave(draft));
    }

    [Fact]
    public void ApplySuggestionToDraft_sets_coordinates_and_label()
    {
        var draft = new OnboardingProfileDraft { Postcode = "2671 GB" };
        var suggestion = new AddressSuggestion("2671 GB Naaldwijk", 51.99, 4.21);
        CandidateHomeAddressLookup.ApplySuggestionToDraft(draft, suggestion);
        Assert.Equal(51.99, draft.HomeLat);
        Assert.Equal(4.21, draft.HomeLng);
        Assert.Equal("2671 GB Naaldwijk", draft.AddressQuery);
        Assert.True(draft.PostcodeResolved);
        Assert.Equal("Naaldwijk", draft.City);
    }
}
