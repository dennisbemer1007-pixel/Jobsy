using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class AfterMatchOpenSlotDedupeTests
{
    [Fact]
    public void ByStartUtc_keeps_one_slot_per_start()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var slots = new[]
        {
            new AfterMatchSlotDto(Guid.NewGuid(), start, start.AddMinutes(30), InterviewSlotProposer.Employer, null, false),
            new AfterMatchSlotDto(Guid.NewGuid(), start, start.AddMinutes(30), InterviewSlotProposer.Employer, null, true),
            new AfterMatchSlotDto(Guid.NewGuid(), start.AddDays(1), start.AddDays(1).AddMinutes(30), InterviewSlotProposer.Employer, null, false),
        };

        var deduped = AfterMatchOpenSlotDedupe.ByStartUtc(slots);
        Assert.Equal(2, deduped.Count);
        Assert.Equal(start, deduped[0].StartUtc);
    }
}
