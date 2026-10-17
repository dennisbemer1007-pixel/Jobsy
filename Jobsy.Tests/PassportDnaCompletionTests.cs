using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class PassportDnaCompletionTests
{
    [Fact]
    public void OpenForWork_gate_keeps_match_when_availability_is_known()
    {
        var ctx = new ProfileVacancyMatchContext
        {
            UserId = Guid.NewGuid(),
            Prefs = new CandidatePreferencesDto(
                Roles: [],
                MaxTravelMinutes: null,
                PreferredTransport: null,
                Availability: new Dictionary<string, string[]> { ["ma"] = ["ochtend"] }),
            OpenForWork = false
        };

        Assert.False(CandidateOpenForWorkGate.HidesMatchScores(ctx));
    }

    [Fact]
    public void OpenForWork_gate_hides_match_when_no_availability_and_not_open()
    {
        var ctx = new ProfileVacancyMatchContext
        {
            UserId = Guid.NewGuid(),
            Prefs = new CandidatePreferencesDto(
                Roles: [],
                MaxTravelMinutes: null,
                PreferredTransport: null,
                Availability: new Dictionary<string, string[]>()),
            OpenForWork = false
        };

        Assert.True(CandidateOpenForWorkGate.HidesMatchScores(ctx));
    }
}
