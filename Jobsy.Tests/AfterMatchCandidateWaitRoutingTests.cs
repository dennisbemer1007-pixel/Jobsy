using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Applications;

namespace Jobsy.Tests;

public sealed class AfterMatchCandidateWaitRoutingTests
{
    [Fact]
    public void RescheduleRequested_routes_to_wait_not_pick_slot()
    {
        var thread = new AfterMatchThreadDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InterviewStatus.RescheduleRequested,
            AfterMatchContactShareState.From(InterviewStatus.RescheduleRequested, null),
            null,
            "Vacancy",
            "Company",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Candidate",
            null,
            DateTime.UtcNow.AddDays(-1),
            null);

        Assert.Equal(AfterMatchPanelRouting.WaitSsrSuffix, AfterMatchPanelRouting.DispatchSuffixForThread(thread));
        Assert.False(AfterMatchCandidateSlotRules.MayPickOpenSlots(thread.InterviewStatus));
    }

    [Fact]
    public void Proposing_with_slots_routes_to_pick_slot()
    {
        var slot = new AfterMatchSlotDto(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddMinutes(30),
            InterviewSlotProposer.Employer,
            null,
            false);
        var thread = new AfterMatchThreadDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "Vacancy",
            "Company",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            null,
            null,
            [slot],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Candidate",
            null,
            DateTime.UtcNow.AddDays(-1),
            null);

        Assert.Equal(AfterMatchPanelRouting.PickSlotSsrSuffix, AfterMatchPanelRouting.DispatchSuffixForThread(thread));
    }
}
