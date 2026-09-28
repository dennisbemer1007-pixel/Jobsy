using Jobsy.Core.Privacy;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class GratisDnaMergeProfileRulesTests
{
    [Fact]
    public void Profile_rules_match_candidate_consent_rules()
    {
        var minor = new MeProfile
        {
            DateOfBirth = new DateOnly(2012, 1, 1)
        };
        Assert.False(GratisDnaMergeProfileRules.CanUseCandidateFeatures(minor, new DateOnly(2026, 9, 27)));

        minor.ParentalConsentAt = DateTime.UtcNow;
        Assert.True(GratisDnaMergeProfileRules.CanUseCandidateFeatures(minor, new DateOnly(2026, 9, 27)));

        var adult = new MeProfile
        {
            DateOfBirth = new DateOnly(2000, 1, 1),
            TestAiConsentAt = DateTime.UtcNow,
            TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
        };
        Assert.True(GratisDnaMergeProfileRules.HasCurrentTestAiConsent(adult));
    }

    [Fact]
    public void Server_answer_parser_keeps_valid_likert_values_only()
    {
        var parsed = GratisDnaServerAnswerParser.Parse(new Dictionary<string, int>
        {
            ["1"] = 4,
            ["bad"] = 3,
            ["6"] = 0,
            ["7"] = 5
        });

        Assert.Equal(2, parsed.Count);
        Assert.Equal(4, parsed[1]);
        Assert.Equal(5, parsed[7]);
    }
}
