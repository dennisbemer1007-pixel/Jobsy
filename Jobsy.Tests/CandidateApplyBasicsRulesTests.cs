using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Tests;

public class CandidateApplyBasicsRulesTests
{
    [Fact]
    public void Empty_profile_has_multiple_gaps()
    {
        var user = new User { Role = UserRole.Candidate, Email = "a@b.nl" };
        var prefs = new CandidatePreferencesDto([], null, null);
        var gaps = CandidateApplyBasicsRules.Evaluate(user, prefs);
        Assert.Contains(gaps, g => g.Field == ApplyBasicsField.Location);
        Assert.Contains(gaps, g => g.Field == ApplyBasicsField.Availability);
        Assert.False(CandidateApplyBasicsRules.IsReady(user, prefs));
    }

    [Fact]
    public void Basics_only_profile_is_ready_without_tests_or_about_me()
    {
        var user = new User
        {
            Role = UserRole.Candidate,
            Email = "a@b.nl",
            FirstName = "E2e",
            LastName = "Basics",
            DateOfBirth = new DateOnly(2000, 1, 1),
            OpenForWork = true,
            TermsAcceptedAt = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            HomeLocation = new GeoPoint(52.0, 4.0)
        };
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 30,
            PreferredTransport: "Fiets",
            Availability: new Dictionary<string, string[]> { ["ma"] = ["ochtend"] },
            HomeAddress: "Straat 1, 1234 AB Den Haag",
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 24,
            WorkStatuses: ["zoekend"]);
        Assert.True(CandidateApplyBasicsRules.IsReady(user, prefs));
    }
}
