using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

public static class AfterMatchOpenSlotDedupe
{
    public static IReadOnlyList<AfterMatchSlotDto> ByStartUtc(IEnumerable<AfterMatchSlotDto> slots) =>
        slots
            .GroupBy(s => s.StartUtc)
            .Select(g => g.First())
            .OrderBy(s => s.StartUtc)
            .ToList();
}
